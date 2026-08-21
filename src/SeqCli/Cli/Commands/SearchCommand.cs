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
using System.Globalization;
using System.Threading.Tasks;
using Seq.Api.Model.Signals;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Output;
using SeqCli.Signals;
using SeqCli.Util;
using Serilog;

// ReSharper disable UnusedType.Global

namespace SeqCli.Cli.Commands;

[Command("search", "Retrieve log events that match a given filter",
    Example = "seqcli search -f \"@Exception like '%TimeoutException%'\" -c 30")]
class SearchCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly DateRangeFeature _range;
    readonly SignalExpressionFeature _signal;
    readonly StoragePathFeature _storagePath;
    readonly List<string> _columns = [];
    string? _filter;
    int _count = 1;
    int _httpClientTimeout = 100000;
    bool _trace, _noWebSockets, _noSignalColumns;

    public SearchCommand()
    {
        Options.Add(
            "f=|filter=",
            "A filter to apply to the search, for example `Host = 'xmpweb-01.example.com'`",
            v => _filter = v);
        
        Options.Add(
            "c=|count=",
            $"The maximum number of events to retrieve; the default is {_count}",
            v => _count = int.Parse(v, CultureInfo.InvariantCulture));

        Options.Add(
            "column=",
            "A column to display preceding each event's message; any Seq expression can be supplied, for " +
            "example `OrderId`, `@SpanKind`, or `@Resource.service.name`; this argument can be used multiple " +
            "times, adding columns in order; applies to plain-text output only",
            c => _columns.Add(ArgumentString.Normalize(c) ?? throw new ArgumentException("Columns require a value.")));

        _range = Enable<DateRangeFeature>();
        _output = Enable(new OutputFormatFeature(supportNative: true, supportJson: true));
        _storagePath = Enable<StoragePathFeature>();
        _signal = Enable<SignalExpressionFeature>();

        Options.Add(
            "request-timeout=",
            $"The time allowed for retrieving each page of events, in milliseconds; the default is {_httpClientTimeout}",
            v => _httpClientTimeout = int.Parse(v.Trim()));

        Options.Add("trace", "Enable detailed (server-side) query tracing", _ => _trace = true);

        Options.Add("no-websockets", "Do not use WebSocket-driven streaming searches", _ => _noWebSockets = true);

        Options.Add("no-signal-columns", "Do not show columns associated with the specified signal expression", _ => _noSignalColumns = true);

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            var config = RuntimeConfigurationLoader.Load(_storagePath);

            var connection = SeqConnectionFactory.Connect(_connection, config);
            connection.Client.HttpClient.Timeout = TimeSpan.FromMilliseconds(_httpClientTimeout);

            var collectedColumns = new List<string>();
            if (!_noSignalColumns && _signal.Signal is { } signalExpression)
            {
                foreach (var signalId in signalExpression.ReferencedSignalIds())
                {
                    var signal = await connection.Signals.FindAsync(signalId);
                    foreach (var column in signal.Columns)
                    {
                        collectedColumns.Add(column.Expression);
                    }
                }
            }

            collectedColumns.AddRange(_columns);
            
            EventColumns? columns = null;
            if (collectedColumns.Count > 0 && !EventColumns.TryCreate(collectedColumns, out columns, out var error))
            {
                Log.Error("The column expression could not be compiled: {Error}", error);
                return 1;
            }

            var output = _output.GetOutputFormat(config, columns?.OutputTemplate(), columns);

            string? filter = null;
            if (!string.IsNullOrWhiteSpace(_filter))
                filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression;

            try
            {
                if (!_noWebSockets)
                {
                    await foreach (var evt in connection.Events.EnumerateAsync(null,
                                       _signal.Signal,
                                       filter,
                                       _count,
                                       fromDateUtc: _range.Start,
                                       toDateUtc: _range.End,
                                       trace: _trace,
                                       render: output.RequiresRender))
                    {
                        output.WriteEventEntity(evt);
                    }

                    return 0;
                }
            }
            catch (NotSupportedException nse)
            {
                Log.Information(nse, "WebSockets not supported; falling back to paged search");
            }
            
            await foreach (var evt in connection.Events.PagedEnumerateAsync(null,
                               _signal.Signal,
                               filter,
                               _count,
                               fromDateUtc: _range.Start,
                               toDateUtc: _range.End,
                               trace: _trace,
                               render: output.RequiresRender))
            {
                output.WriteEventEntity(evt);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not retrieve search result: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}