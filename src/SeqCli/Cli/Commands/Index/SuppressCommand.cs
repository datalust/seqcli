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
using Seq.Api.Model.Indexes;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Cli.Commands.Index;

[Command("index", "suppress", "Suppress an index", Example="seqcli index suppress -i index-2191448f1d9b4f22bd32c6edef752748")]
class SuppressCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    
    string? _id;

    public SuppressCommand()
    {
        Options.Add(
            "i=|id=",
            "The id of an index to suppress",
            id => _id = id);
        
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_id == null)
        {
            Log.Error("An `id` must be specified");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);
        var toSuppress = await connection.Indexes.FindAsync(_id);
        await connection.Indexes.SuppressAsync(toSuppress);

        return 0;
    }
}