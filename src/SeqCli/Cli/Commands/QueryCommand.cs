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
using System.Collections.Generic;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands
{
    [Command("query", "Execute an SQL query and receive results in CSV format",
        Example = "seqcli query -q \"select count(*) from stream group by @Level\" --start=\"2018-02-28T13:00Z\"")]
    class QueryCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        readonly DateRangeFeature _range;
        readonly SignalExpressionFeature _signal;
        string _query;
        int? _timeoutMS;

        public QueryCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            Options.Add("q=|query=", "The query to execute", v => _query = v);
            _range = Enable<DateRangeFeature>();
            _signal = Enable<SignalExpressionFeature>();
            Options.Add("timeout=", "The query execution timeout in milliseconds", v => _timeoutMS = int.Parse(v));
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

            // The `rangeStartUtc` parameter of `QueryCsvAsync()` should now be optional; we can
            // remove the `.Value` when _Seq.Api_ is updated to reflect this.
            // ReSharper disable once PossibleInvalidOperationException
            var result = await QueryCsvAsync(connection, _query, _range.Start.Value, _range.End, _signal.Signal, _timeoutMS);
            Console.WriteLine(result);

            return 0;
        }

        static async Task<string> QueryCsvAsync(SeqConnection connection, string query, DateTime? rangeStartUtc, DateTime? rangeEndUtc, SignalExpressionPart signalExpression, int? timeoutMS)
        {
            // From dates should no longer be mandatory for QueryCsvAsync (issue raised)

            var parameters = new Dictionary<string, object>
            {
                ["q"] = query,
                ["format"] = "text/csv"
            };

            if (rangeStartUtc.HasValue)
                parameters.Add(nameof(rangeEndUtc), rangeStartUtc.Value);
            if (rangeEndUtc.HasValue)
                parameters.Add(nameof(rangeEndUtc), rangeEndUtc.Value);
            if (signalExpression != null)
                parameters.Add("signal", signalExpression.ToString());
            if (timeoutMS.HasValue)
                parameters.Add("timeoutMS", timeoutMS.Value.ToString("0"));
            var body = new SignalEntity();

            var drg = await connection.LoadResourceGroupAsync("Data");

            return await connection.Client.PostReadStringAsync(drg, "Query", body, parameters);
        }
    }
}
