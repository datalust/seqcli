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
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using Serilog;

// ReSharper disable UnusedType.Global

namespace SeqCli.Cli.Commands;

[Command("query", "Execute an SQL query and receive results in CSV format",
    Example = "seqcli query -q \"select count(*) from stream group by @Level\" --start=\"2018-02-28T13:00Z\"")]
class QueryCommand : Command
{
    readonly OutputFormatFeature _output;
    readonly ConnectionFeature _connection;
    readonly DateRangeFeature _range;
    readonly SignalExpressionFeature _signal;
    readonly TimeoutFeature _timeout;
    readonly StoragePathFeature _storagePath;
    string? _query;
    bool _trace;

    public QueryCommand()
    {
        Options.Add("q=|query=", "The query to execute", v => _query = v);
        _range = Enable<DateRangeFeature>();
        _signal = Enable<SignalExpressionFeature>();
        _timeout = Enable<TimeoutFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        Options.Add("trace", "Enable detailed (server-side) query tracing", _ => _trace = true);
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            Log.Error("A query must be specified");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var timeout = _timeout.ApplyTimeout(connection.Client.HttpClient);

        var output = _output.GetOutputFormat(config);
        if (output.Json)
        {
            var result = await connection.Data.QueryAsync(_query, _range.Start, _range.End, _signal.Signal, timeout: timeout, trace: _trace);

            // Some friendlier JSON output is definitely possible here
            Console.WriteLine(JsonConvert.SerializeObject(result));
        }
        else
        {
            var result = await connection.Data.QueryCsvAsync(_query, _range.Start, _range.End, _signal.Signal, timeout: timeout, trace: _trace);
            output.WriteCsv(result);
        }

        return 0;
    }
}