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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Compact.Reader;

namespace SeqCli.Cli.Commands
{
    [Command("tail", "Stream log events matching a filter")]
    class TailCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly ConnectionFeature _connection;
        string _filter;
        bool _json;

        public TailCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            _connection = Enable<ConnectionFeature>();
            Options.Add(
                "f=|filter=",
                "An optional filter to apply to the stream, for example `@Level = 'Error'`",
                v => _filter = v);
            Options.Add(
                "json",
                "Print the streamed events in newline-delimited JSON (the default is plain text)",
                v => _json = true);
        }

        protected override async Task<int> Run()
        {
            var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (s,a) => cancel.Cancel();
            
            var connection = _connectionFactory.Connect(_connection);

            string strict = null;
            if (!string.IsNullOrWhiteSpace(_filter))
            {
                var converted = await connection.Expressions.ToStrictAsync(_filter);
                strict = converted.StrictExpression;
            }

            var outputConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LevelAlias.Minimum);
            
            if (_json)
                outputConfiguration.WriteTo.Console(new RenderedCompactJsonFormatter());
            else
                outputConfiguration.WriteTo.Console();

            using (var output = outputConfiguration.CreateLogger())
            using (var stream = await connection.Events.StreamAsync<JObject>(filter: strict))
            {
                var subscription = stream
                    .Select(LogEventReader.ReadFromJObject)
                    .Subscribe(evt => output.Write(evt), () => cancel.Cancel());

                cancel.Token.WaitHandle.WaitOne();
                subscription.Dispose();
            }

            return 0;
        }
    }
}