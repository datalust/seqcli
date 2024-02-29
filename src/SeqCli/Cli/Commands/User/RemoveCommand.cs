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
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.User;

[Command("user", "remove", "Remove a user from the server",
    Example="seqcli user remove -n alice")]
class RemoveCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly UserIdentityFeature _userIdentity;
    readonly ConnectionFeature _connection;

    public RemoveCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _userIdentity = Enable(new UserIdentityFeature("remove"));
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_userIdentity.Name == null && _userIdentity.Id == null)
        {
            Log.Error("A `name` or `id` must be specified");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var toRemove = _userIdentity.Id != null ? [await connection.Users.FindAsync(_userIdentity.Id)]
            :
            (await connection.Users.ListAsync())
            .Where(u => _userIdentity.Name == u.Username) 
            .ToArray();

        if (!toRemove.Any())
        {
            Log.Error("No matching user was found");
            return 1;
        }

        foreach (var userEntity in toRemove)
            await connection.Users.RemoveAsync(userEntity);

        return 0;
    }
}