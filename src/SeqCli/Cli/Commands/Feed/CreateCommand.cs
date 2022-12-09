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
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Feed
{
    [Command("feed", "create", "Create a NuGet feed",
        Example = "seqcli feed create -n 'CI' --location=\"https://f.feedz.io/example/ci\" -u Seq --password-stdin")]
    class CreateCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;

        readonly ConnectionFeature _connection;
        readonly OutputFormatFeature _output;

        string? _name, _location, _username, _password;
        bool _passwordStdin;

        public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "n=|name=",
                "A unique name for the feed",
                n => _name = ArgumentString.Normalize(n));
            
            Options.Add(
                "l=|location=",
                "The feed location; this may be a NuGet v2 or v3 feed URL, or a local filesystem path on the Seq server",
                l => _location = ArgumentString.Normalize(l));
            
            Options.Add(
                "u=|username=",
                "The username Seq should supply when connecting to the feed, if authentication is required",
                n => _username = ArgumentString.Normalize(n));

            Options.Add(
                "p=|password=",
                "A feed password, if authentication is required; note that `--password-stdin` is more secure",
                p => _password = ArgumentString.Normalize(p));

            Options.Add(
                "password-stdin",
                "Read the feed password from `STDIN`",
                _ => _passwordStdin = true);

            _connection = Enable<ConnectionFeature>();
            _output = Enable(new OutputFormatFeature(config.Output));
        }

        protected override async Task<int> Run()
        {
            var connection = _connectionFactory.Connect(_connection);

            var feed = await connection.Feeds.TemplateAsync();
            feed.Name = _name;
            feed.Location = _location;
            feed.Username = _username;

            if (_passwordStdin)
            {
                if (_password != null)
                {
                    Log.Error("The `password` and `password-stdin` options are mutually exclusive");
                    return 1;
                }

                _password = await Console.In.ReadLineAsync();
            }

            if (_password != null)
            {
                feed.NewPassword = _password;
            }
            
            feed = await connection.Feeds.AddAsync(feed);

            _output.WriteEntity(feed);

            return 0;
        }
    }
}
