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

[Command("config", "get", "View a field from the `SeqCli.json` file")]
class GetCommand : Command
{
    readonly StoragePathFeature _storagePath;
    readonly ConfigKeyFeature _key;

    public GetCommand()
    {
        _storagePath = Enable<StoragePathFeature>();
        _key = Enable<ConfigKeyFeature>();
    }

    protected override Task<int> Run()
    {
        var config = SeqCliConfig.ReadFromFile(_storagePath.ConfigFilePath);
        if (!KeyValueSettings.TryGetValue(config, _key.GetKey(), out var value, out _))
            throw new ArgumentException($"Field `{_key.GetKey()}` not found.");
        
        Console.WriteLine(value);
        return Task.FromResult(0);
    }
}