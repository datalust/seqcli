// Copyright Datalust Pty Ltd and Contributors
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
using System.IO;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Templates.Files;
using SeqCli.Templates.Sets;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.Sample
{
    [Command("sample", "setup", "Configure a Seq instance with sample dashboards, signals, users, and so on",
        Example = "seqcli sample setup")]
    class SetupCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly ConfirmFeature _confirm;

        public SetupCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            // The command will also at some point accept an `--allow-outbound-requests` flag, which will cause sample
            // apps to be installed, and a health check to be set up.
            
            _confirm = Enable<ConfirmFeature>();
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var (url, _) = _connectionFactory.GetConnectionDetails(_connection);
            if (!_confirm.TryConfirm($"This will apply sample configuration items to the Seq server at {url}."))
            {
                await Console.Error.WriteLineAsync("Canceled by user.");
                return 1;
            }

            var templatesPath = Content.GetPath(Path.Combine("Sample", "Templates"));
            var templateFiles = Directory.GetFiles(templatesPath);
            var templates = new List<EntityTemplateFile>();
            foreach (var templateFile in templateFiles)
            {
                if (!EntityTemplateFileLoader.Load(templateFile, out var template, out var error))
                {
                    await Console.Error.WriteLineAsync(error);
                    return 1;
                }
                
                templates.Add(template);
            }

            var err = await EntityTemplateSet.ApplyAsync(templates, connection);
            if (err != null)
            {
                await Console.Error.WriteLineAsync(err);
                return 1;
            }
            
            return 0;
        }
    }
}
