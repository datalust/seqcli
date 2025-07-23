// Copyright © Datalust Pty Ltd and Contributors
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

// ReSharper disable once UnusedType.Global

namespace SeqCli.Cli.Commands.Config;

[Command("config", "list", "View all fields in the `SeqCli.json` file")]
class ListCommand : Command
{
    readonly StoragePathFeature _storagePath;

    public ListCommand()
    {
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override Task<int> Run()
    {
        var config = SeqCliConfig.ReadFromFile(_storagePath.ConfigFilePath);
        foreach (var (key, value, _) in KeyValueSettings.Inspect(config))
        {
            Console.WriteLine($"{key}={value}");
        }
        return Task.FromResult(0);
    }
}