﻿// Copyright 2018 Datalust Pty Ltd
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Events;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Levels;
using SeqCli.Util;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;
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
    string? _filter;
    int _count = 1;
    int _httpClientTimeout = 100000;
    bool _trace, _noWebSockets;

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

        _range = Enable<DateRangeFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _signal = Enable<SignalExpressionFeature>();

        Options.Add(
            "request-timeout=",
            $"The time allowed for retrieving each page of events, in milliseconds; the default is {_httpClientTimeout}",
            v => _httpClientTimeout = int.Parse(v.Trim()));

        Options.Add("trace", "Enable detailed (server-side) query tracing", _ => _trace = true);

        Options.Add("no-websockets", "Do not use WebSocket-driven streaming searches", _ => _noWebSockets = true);

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            var config = RuntimeConfigurationLoader.Load(_storagePath);
            await using var output = _output.GetOutputFormat(config).CreateOutputLogger();
            var connection = SeqConnectionFactory.Connect(_connection, config);
            connection.Client.HttpClient.Timeout = TimeSpan.FromMilliseconds(_httpClientTimeout);

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
                                       trace: _trace))
                    {
                        output.Write(ToSerilogEvent(evt));
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
                               trace: _trace))
            {
                output.Write(ToSerilogEvent(evt));
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not retrieve search result: {ErrorMessage}", ex.Message);
            return 1;
        }
    }

    LogEvent ToSerilogEvent(EventEntity evt)
    {
        return new LogEvent(
            DateTimeOffset.ParseExact(evt.Timestamp, "o", CultureInfo.InvariantCulture).ToLocalTime(),
            LevelMapping.ToSerilogLevel(evt.Level),
            string.IsNullOrWhiteSpace(evt.Exception) ? null : new TextException(evt.Exception),
            new MessageTemplate(evt.MessageTemplateTokens.Select(ToMessageTemplateToken)),
            evt.Properties
                .Select(p => CreateProperty(p.Name, p.Value))
        );
    }

    static MessageTemplateToken ToMessageTemplateToken(MessageTemplateTokenPart token)
    {
        // Not ideal, we lose renderings, alignment etc. here.

        if (token.Text != null)
            return new TextToken(token.Text);
        return new PropertyToken(token.PropertyName, token.RawText ?? $"{{{token.PropertyName}}}");
    }

    LogEventProperty CreateProperty(string name, object value)
    {
        return LogEventPropertyFactory.SafeCreate(name, CreatePropertyValue(value));
    }

    LogEventPropertyValue CreatePropertyValue(object value)
    {
        switch (value)
        {
            case JObject jo:
                jo.TryGetValue("$typeTag", out var tt);
                return new StructureValue(
                    jo.Properties()
                        .Where(kvp => kvp.Name != "$typeTag")
                        .Select(kvp => CreateProperty(kvp.Name, kvp.Value)),
                    (tt as JValue)?.Value as string);

            case JArray ja:
                return new SequenceValue(ja.Select(CreatePropertyValue));

            default:
                return new ScalarValue(value);
        }
    }
}