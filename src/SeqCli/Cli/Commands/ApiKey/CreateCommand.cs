// Copyright 2018 Datalust Pty Ltd and Contributors
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
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.Inputs;
using Seq.Api.Model.LogEvents;
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Levels;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.ApiKey
{
    [Command("apikey", "create", "Create an API key for ingestion",
        Example = "seqcli apikey create -t 'Test API Key' -p Environment=Test")]
    class CreateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly PropertiesFeature _properties;
        readonly OutputFormatFeature _output;

        string _title, _token, _filter, _level;
        bool _useServerTimestamps;

        public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "t=|title=",
                "A title for the API key",
                t => _title = ArgumentString.Normalize(t));

            Options.Add(
                "token=",
                "A pre-allocated API key token; by default, a new token will be generated and written to `STDOUT`",
                t => _token = ArgumentString.Normalize(t));

            _properties = Enable<PropertiesFeature>();

            Options.Add(
                "filter=",
                "A filter to apply to incoming events",
                f => _filter = ArgumentString.Normalize(f));

            Options.Add(
                "minimum-level=",
                "The minimum event level/severity to accept; the default is to accept all events",
                v => _level = ArgumentString.Normalize(v));

            Options.Add(
                "use-server-timestamps",
                "Discard client-supplied timestamps and use server clock values",
                _ => _useServerTimestamps = true);

            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            // Default will apply the ingest permission
            var apiKey = await connection.ApiKeys.TemplateAsync();

            apiKey.Title = _title;
            apiKey.AppliedProperties = _properties.Properties
                .Select(kvp => new InputAppliedPropertyPart {Name = kvp.Key, Value = kvp.Value})
                .ToList();
            apiKey.UseServerTimestamps = _useServerTimestamps;

            // If _token is null, a value will be returned when the key is created
            apiKey.Token = _token;

            if (_filter != null)
            {
                apiKey.InputFilter = new SignalFilterPart
                {
                    Filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression,
                    FilterNonStrict = _filter
                };
            }

            if (_level != null)
            {
                apiKey.MinimumLevel = Enum.Parse<LogEventLevel>(LevelMapping.ToFullLevelName(_level));
            }

            apiKey = await connection.ApiKeys.AddAsync(apiKey);

            if (_token == null && !_output.Json)
            {
                Console.WriteLine(apiKey.Token);
            }
            else
            {
                _output.WriteEntity(apiKey);
            }

            return 0;
        }
    }
}
