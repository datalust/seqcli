// Copyright 2026 Datalust Pty Ltd and Contributors
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
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using Serilog;
// ReSharper disable UnusedType.Global

namespace SeqCli.Cli.Commands;

[Command("events", "delete", "Delete log events that match a given date range or filter",
    Example = "seqcli events delete --start \"2026-01-01T00:00:00Z\" --end \"2026-01-31T23:59:59Z\"")]
class DeleteCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly DateRangeFeature _range;
    readonly SignalExpressionFeature _signal;
    readonly StoragePathFeature _storagePath;
    string? _filter;

    public DeleteCommand()
    {
        Options.Add(
            "f=|filter=",
            "A filter to apply to deletion, for example `Host = 'xmpweb-01.example.com'`",
            v => _filter = v);

        _range = Enable<DateRangeFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _signal = Enable<SignalExpressionFeature>();

        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var connection = SeqConnectionFactory.Connect(_connection, config);

            string? filter = null;
            if (!string.IsNullOrWhiteSpace(_filter))
                filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression;

            await connection.Events.DeleteAsync(
                null,
                _signal.Signal,
                filter,
                _range.Start,
                _range.End,
                null);

            Log.Information("Deleted matching events");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not delete matching events: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}