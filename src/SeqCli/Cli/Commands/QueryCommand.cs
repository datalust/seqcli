// Copyright 2016-2017 Datalust Pty Ltd
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
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands
{
    [Command("query", "Execute SQL queries to retrieve CSV results")]
    class QueryCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        readonly DateRangeFeature _range;
        string _query;

        public QueryCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _connection = Enable<ConnectionFeature>();
            Options.Add("q=|query=", "The query to execute, for example `select count(*) from stream`", v => _query = v);
            _range = Enable<DateRangeFeature>();
        }

        protected override async Task<int> Run()
        {
            if (string.IsNullOrWhiteSpace(_query))
            {
                Console.Error.WriteLine("A query must be specified.");
                return 1;
            }

            var config = SeqCliConfig.Read();
            var connection = _connectionFactory.Connect(_connection, config);

            // The `rangeStartUtc` parameter of `QueryCsvAsync()` should now be optional; we can
            // remove the `.Value` when _Seq.Api_ is updated to reflect this.
            // ReSharper disable once PossibleInvalidOperationException
            var result = await connection.Data.QueryCsvAsync(_query, _range.Start.Value, _range.End);
            Console.WriteLine(result);

            return 0;
        }
    }
}
