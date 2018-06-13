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
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Signal
{
    [Command("signal", "import", "Import signals in newline-delimited JSON format",
        Example="seqcli signal import -i ./Exceptions.json")]
    class ImportCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly FileInputFeature _fileInputFeature;
        readonly ConnectionFeature _connection;
        
        readonly JsonSerializer _serializer = JsonSerializer.Create(
            new JsonSerializerSettings{
                Converters = { new StringEnumConverter() }
            });

        public ImportCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _fileInputFeature = Enable(new FileInputFeature("File to import"));
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            using (var input = _fileInputFeature.OpenInput())
            {
                var line = input.ReadLine();
                while (line != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Explicitly copying fields here ensures we don't try copying links or ids; for other
                        // entity types it'll ensure we notice places that "referential integrity" has to be
                        // maintained.
                        var src = _serializer.Deserialize<SignalEntity>(new JsonTextReader(new StringReader(line)));
                        var dest = await connection.Signals.TemplateAsync();
                        dest.Title = src.Title;
                        dest.Description = src.Description;
                        dest.ExplicitGroupName = src.ExplicitGroupName;
                        dest.Grouping = src.Grouping;
                        dest.IsProtected = src.IsProtected;
                        dest.Filters = src.Filters;
                        dest.TaggedProperties = src.TaggedProperties;
                        await connection.Signals.AddAsync(dest);
                    }

                    line = input.ReadLine();
                }
            }

            return 0;
        }
    }
}