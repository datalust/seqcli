// Copyright 2020 Datalust Pty Ltd and Contributors
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
using SeqCli.Apps.Definitions;
using SeqCli.Apps.Hosting;
using SeqCli.Config;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.App
{
    [Command("app", "define", "Generate an app definition for a .NET `[SeqApp]` plug-in",
        Example = "seqcli app define -d \"./bin/Debug/netstandard2.2\"")]
    class DefineCommand : Command
    {
        string _dir = Environment.CurrentDirectory, _type;

        public DefineCommand(SeqCliConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            Options.Add(
                "d=|directory=",
                "The directory containing .NET Standard assemblies; defaults to the current directory",
                d => _dir = ArgumentString.Normalize(d) ?? _dir);

            Options.Add(
                "t|type=",
                "The [SeqApp] plug-in type name; defaults to scanning assemblies for a single type marked with this attribute",
                t => _type = ArgumentString.Normalize(t));
        }

        protected override Task<int> Run()
        {
            var configuration = PackageInterrogator.FindAppConfiguration(_dir, _type);

            if (configuration == null)
            {
                Console.Error.WriteLine("No type marked with `[SeqApp]` could be found.");
                return Task.FromResult(1);
            }

            Console.WriteLine(configuration);
            return Task.FromResult(0);
        }
    }
}
