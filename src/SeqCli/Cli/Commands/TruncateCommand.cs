// Copyright 2018 Datalust Pty Ltd
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

namespace SeqCli.Cli.Commands
{
    [Command("query", "Execute an SQL query and receive results in CSV format",
        Example = "seqcli truncate -q \"select count(*) from stream group by @Level\" --start=\"2018-02-28T13:00Z\"")]
    class TruncateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        readonly DateRangeFeature _range;
        readonly SignalExpressionFeature _signal;
        readonly TimeoutFeature _timeout;
        string _query;

        public TruncateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            Options.Add("q=|query=", "The query to execute", v => _query = v);
            _range = Enable<DateRangeFeature>();
            _signal = Enable<SignalExpressionFeature>();
            _timeout = Enable<TimeoutFeature>();
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            if (string.IsNullOrWhiteSpace(_query))
            {
                Log.Error("A query must be specified");
                return 1;
            }

            var connection = _connectionFactory.Connect(_connection);

            await connection.Events.DeleteInSignalAsync(
                filter: _query,
                fromDateUtc: _range.Start,
                toDateUtc: _range.End,
                signal: _signal.Signal);

            return 0;
        }
    }
}
