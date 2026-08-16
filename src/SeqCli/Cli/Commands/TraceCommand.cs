// Copyright © Datalust and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Traces;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands;

[Command("trace", "Display a trace as a tree of spans",
    Example = "seqcli trace -i 7d4dedcc73b18e449e0e4ea08cbe346d --column @SpanId --logs")]
class TraceCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    readonly List<string> _columns = [];
    string? _id;
    string? _spanId;
    bool _includeLogs;
    bool _includeExceptions;
    bool _json;

    public TraceCommand()
    {
        Options.Add(
            "i=|id=",
            "The id of the trace to display",
            id => _id = ArgumentString.Normalize(id));

        Options.Add(
            "span-id=",
            "The id of a span within the trace; when specified, only the subtree rooted at this span is shown",
            spanId => _spanId = ArgumentString.Normalize(spanId));

        Options.Add(
            "column=",
            "A column to display preceding each event's message; any Seq expression can be supplied, for " +
            "example `OrderId`, `@SpanKind`, or `@Resource.service.name`; this argument can be used multiple " +
            "times, adding columns in order",
            c => _columns.Add(ArgumentString.Normalize(c) ?? throw new ArgumentException("Columns require a value.")));

        Options.Add(
            "logs",
            "Include log events in the trace, in addition to spans",
            _ => _includeLogs = true);

        Options.Add(
            "exceptions",
            "Include exception details, where present",
            _ => _includeExceptions = true);

        Options.Add(
            "json",
            "Print the trace as a single JSON document, with spans and log events nested under " +
            "their parents (the default is plain text)",
            _ => _json = true);

        _output = Enable(new OutputFormatFeature(supportNative: false, supportJson: false));
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            if (_id == null)
            {
                Log.Error("A trace id must be specified");
                return 1;
            }

            var traceId = _id.ToLowerInvariant();
            if (!TraceQuery.IsValidTraceId(traceId))
            {
                Log.Error("The trace id {TraceId} is not valid; trace ids are 32 hexadecimal digits", _id);
                return 1;
            }

            var spanId = _spanId?.ToLowerInvariant();
            if (spanId != null && !TraceQuery.IsValidSpanId(spanId))
            {
                Log.Error("The span id {SpanId} is not valid; span ids are 16 hexadecimal digits", _spanId);
                return 1;
            }

            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var connection = SeqConnectionFactory.Connect(_connection, config);

            var result = await connection.Data.TryQueryAsync(TraceQuery.Build(traceId, _includeLogs, _includeExceptions, _columns));
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Log.Error("Could not retrieve trace: {ErrorMessage}", result.Error);
                foreach (var reason in result.Reasons)
                    Log.Error("{Reason}", reason);
                return 1;
            }

            var traceEvents = TraceQuery.ReadEvents(result, _includeExceptions, _columns);
            if (traceEvents.Count == 0)
            {
                Log.Error("No events found for trace {TraceId}", traceId);
                return 1;
            }

            var complete = traceEvents.Count != TraceQuery.MaxEvents;
            if (!complete)
                Log.Warning("Only the first {Count} events in the trace were retrieved; the tree may be incomplete",
                    TraceQuery.MaxEvents);

            var roots = TraceTreeBuilder.Build(traceEvents);

            TraceTreeNode? subtreeRoot = null;
            if (spanId != null)
            {
                subtreeRoot = TraceTreeBuilder.FindSpan(roots, spanId);
                if (subtreeRoot == null)
                {
                    Log.Error("The span {SpanId} does not appear in trace {TraceId}", spanId, traceId);
                    return 1;
                }
            }

            if (_json)
            {
                var document = subtreeRoot != null ?
                    TraceTreeJsonFormatter.ToJson(traceId, subtreeRoot, complete, _columns) :
                    TraceTreeJsonFormatter.ToJson(traceId, roots, complete, _columns);

                Console.WriteLine(document.ToString(Formatting.Indented));
            }
            else
            {
                var output = _output.GetOutputFormat(config, TraceShowFormat.OutputTemplate(_columns.Count));
                foreach (var logEvent in TraceTreeFormatter.ToLogEvents(subtreeRoot != null ? [subtreeRoot] : roots))
                    output.WriteLogEvent(logEvent);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not retrieve trace: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}
