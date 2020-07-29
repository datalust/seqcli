// Copyright 2019 Datalust Pty Ltd and Contributors
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
using SeqCli.Apps;
using SeqCli.Apps.Hosting;
using SeqCli.Config;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.App
{
    [Command("app", "run", "Host a .NET `[SeqApp]` plug-in",
        Example = "seqcli tail --json | seqcli app run -d \"./bin/Debug/netstandard2.2\" -p ToAddress=example@example.com")]
    class RunCommand : Command
    {
        string _dir = Environment.CurrentDirectory, _type, _serverUrl, _storage = Environment.CurrentDirectory;
        readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        public RunCommand(SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _serverUrl = config.Connection.ServerUrl;

            Options.Add(
                "d=|directory=",
                "The directory containing .NET Standard assemblies; defaults to the current directory",
                d => _dir = ArgumentString.Normalize(d) ?? _dir);

            Options.Add(
                "t|type=",
                "The [SeqApp] plug-in type name; defaults to scanning assemblies for a single type marked with this attribute",
                t => _type = ArgumentString.Normalize(t));

            Options.Add(
                "p={=}|property={=}",
                "Specify name/value settings for the app, e.g. `-p ToAddress=example@example.com -p Subject=\"Alert!\"`",
                (n, v) =>
                {
                    var name = n.Trim();
                    var valueText = v?.Trim();
                    _settings.Add(name, valueText ?? "");
                });

            Options.Add("s=|server=",
                "The URL of the Seq server, used only for app configuration (no connection is made to the server); by default the `connection.serverUrl` value will be used",
                v => _serverUrl = ArgumentString.Normalize(v) ?? _serverUrl);

            Options.Add(
                "storage=",
                "A directory in which app-specific data can be stored; defaults to the current directory",
                d => _storage = ArgumentString.Normalize(d) ?? _storage);
        }

        protected override async Task<int> Run()
        {
            // Todo - accept settings from the environment.
            return await AppHost.Run(_dir, _settings, _storage, _serverUrl, _type);
        }
    }
}
