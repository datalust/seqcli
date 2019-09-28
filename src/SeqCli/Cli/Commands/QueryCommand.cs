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

namespace SeqCli.Cli.Commands
{
    [Command("query", "Execute an SQL query and receive results in CSV format",
        Example = "seqcli query -q \"select count(*) from stream group by @Level\" --start=\"2018-02-28T13:00Z\"")]
    class QueryCommand : Command
    {
        readonly OutputFormatFeature _output;
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        readonly DateRangeFeature _range;
        readonly SignalExpressionFeature _signal;
        string _query;
        int? _timeoutMS;

        public QueryCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            Options.Add("q=|query=", "The query to execute", v => _query = v);
            _range = Enable<DateRangeFeature>();
            _signal = Enable<SignalExpressionFeature>();
            Options.Add("timeout=", "The query execution timeout in milliseconds", v => _timeoutMS = int.Parse(v?.Trim() ?? "0"));
            _output = Enable(new OutputFormatFeature(config.Output));
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
            
            var timeout = _timeoutMS.HasValue ? TimeSpan.FromMilliseconds(_timeoutMS.Value) : (TimeSpan?)null;
            if (timeout != null)
            {
                // The timeout is applied server-side; allowing an extra 10 seconds here means that the
                // user experience will be consistent - the error message will be the server's message, etc.
                connection.Client.HttpClient.Timeout = timeout.Value.Add(TimeSpan.FromSeconds(10));
            }

            if (_output.Json)
            {
                var result = await connection.Data.QueryAsync(_query, _range.Start, _range.End, _signal.Signal, timeout: timeout);

                // Some friendlier JSON output is definitely possible here
                Console.WriteLine(JsonConvert.SerializeObject(result));
            }
            else
            {
                var result = await connection.Data.QueryCsvAsync(_query, _range.Start, _range.End, _signal.Signal, timeout: timeout);
                _output.WriteCsv(result);
            }

            return 0;
        }
    }
}
