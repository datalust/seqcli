﻿// Copyright © Datalust Pty Ltd and Contributors
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
using Seq.Api.Model.Settings;

namespace SeqCli.Cli.Commands.Settings;

[Command("setting", "names", "Print the names of all supported settings")]
class NamesCommand: Command
{
    protected override Task<int> Run(string[] unrecognized)
    {
        foreach (var name in Enum.GetNames(typeof(SettingName)).Order())
        {
            Console.WriteLine(name);
        }
        
        return Task.FromResult(0);
    }
}