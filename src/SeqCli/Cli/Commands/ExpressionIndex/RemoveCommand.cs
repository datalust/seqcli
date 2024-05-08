﻿// Copyright 2018 Datalust Pty Ltd and Contributors
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
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.ExpressionIndex;

[Command("expressionindex", "remove", "Remove an expression index from the server",
    Example = "seqcli expressionindex -i expressionindex-2529")]
class RemoveCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    string? _id;

    public RemoveCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "i=|id=",
            "The id of an expression index to remove",
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
        var toRemove = await connection.ExpressionIndexes.FindAsync(_id);
        await connection.ExpressionIndexes.RemoveAsync(toRemove);

        return 0;
    }
}