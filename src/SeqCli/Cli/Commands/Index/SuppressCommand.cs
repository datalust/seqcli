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
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.Index;

[Command("index", "suppress", "Suppress index", Example="seqcli index suppress -i signal-6543")]
class SuppressCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;
    readonly ConnectionFeature _connection;
    string? _id;

    public SuppressCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "i=|id=",
            "The id of a signal index to suppress",
            id => _id = id);
        
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_id == null)
        {
            Log.Error("An `id` must be specified");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);
        var toSuppress = await connection.Indexes.FindAsync(_id);
        if (toSuppress.IndexedEntityType != IndexedEntityType.Signal)
        {
            Log.Error("Only Signal indexes may be suppressed; to delete an expression index or an alert index remove the expression index or alert");
            return 1;
        }
        await connection.Indexes.SuppressAsync(toSuppress);

        await Task.Delay(1);
        return 0;
    }
}