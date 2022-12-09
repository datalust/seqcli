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

namespace SeqCli.Cli.Commands.Dashboard;

[Command("dashboard", "remove", "Remove a dashboard from the server",
    Example="seqcli dashboard remove -i dashboard-159")]
class RemoveCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly EntityIdentityFeature _entityIdentity;
    readonly EntityOwnerFeature _entityOwner;
    readonly ConnectionFeature _connection;

    public RemoveCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _entityIdentity = Enable(new EntityIdentityFeature("dashboard", "remove"));
        _entityOwner = Enable(new EntityOwnerFeature("dashboard", "remove", _entityIdentity));
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_entityIdentity.Title == null && _entityIdentity.Id == null)
        {
            Log.Error("A `title` or `id` must be specified");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var toRemove = _entityIdentity.Id != null ?
            new[] { await connection.Dashboards.FindAsync(_entityIdentity.Id) } :
            (await connection.Dashboards.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(dashboard => _entityIdentity.Title == dashboard.Title)
            .ToArray();

        if (!toRemove.Any())
        {
            Log.Error("No matching dashboard was found");
            return 1;
        }

        foreach (var dashboard in toRemove)
            await connection.Dashboards.RemoveAsync(dashboard);

        return 0;
    }
}