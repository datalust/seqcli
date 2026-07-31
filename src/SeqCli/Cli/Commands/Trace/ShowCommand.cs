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
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Traces;
using SeqCli.Util;
using Serilog;

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands.Trace;

[Command("trace", "show", "Display a trace as a tree of spans",
    Example = "seqcli trace show -i 7d4dedcc73b18e449e0e4ea08cbe346d --logs")]
class ShowCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    readonly List<string> _properties = [];
    string? _id;
    bool _includeLogs;

    public ShowCommand()
    {
        Options.Add(
            "i=|id=",
            "The id of the trace to display",
            id => _id = ArgumentString.Normalize(id));

        Options.Add(
            "p=|property=",
            "A property to display following each event's message, for example `OrderId` or " +
            "`@Resource.service.name`; this argument can be used multiple times",
            p => _properties.Add(ArgumentString.Normalize(p) ?? throw new ArgumentException("Properties require a value.")));

        Options.Add(
            "logs",
            "Include log events in the trace, in addition to spans",
            _ => _includeLogs = true);

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

            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var connection = SeqConnectionFactory.Connect(_connection, config);
            var output = _output.GetOutputFormat(config, TraceShowFormat.OutputTemplate);

            var result = await connection.Data.TryQueryAsync(TraceQuery.Build(traceId, _includeLogs, _properties));
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Log.Error("Could not retrieve trace: {ErrorMessage}", result.Error);
                foreach (var reason in result.Reasons)
                    Log.Error("{Reason}", reason);
                return 1;
            }

            var traceEvents = TraceQuery.ReadEvents(result, _properties);
            if (traceEvents.Count == 0)
            {
                Log.Error("No events found for trace {TraceId}", traceId);
                return 1;
            }

            if (traceEvents.Count == TraceQuery.MaxEvents)
                Log.Warning("Only the first {Count} events in the trace were retrieved; the tree may be incomplete",
                    TraceQuery.MaxEvents);

            foreach (var logEvent in TraceTreeFormatter.ToLogEvents(TraceTreeBuilder.Build(traceEvents)))
                output.WriteLogEvent(logEvent);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not retrieve trace: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}
