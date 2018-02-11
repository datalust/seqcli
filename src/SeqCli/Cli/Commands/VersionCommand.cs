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
using System.Reflection;
using System.Threading.Tasks;

namespace SeqCli.Cli.Commands
{
    [Command("version", "Print the current executable version")]
    class VersionCommand : Command
    {
        protected override Task<int> Run()
        {
            var version = GetVersion();
            Console.WriteLine(version);
            return Task.FromResult(0);
        }

        public static string GetVersion()
        {
            return typeof(VersionCommand).GetTypeInfo().Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }
    }
}
