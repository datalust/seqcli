// Copyright © Datalust and contributors.
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

using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Cli.Commands.Alert;

[Command("alert", "remove", "Remove an alert from the server",
    Example = "seqcli alert remove -t 'Too many errors'")]
class RemoveCommand : Command
{
    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly EntityOwnerFeature _entityOwner;
    readonly StoragePathFeature _storagePath;

    public RemoveCommand()
    {
        _entityIdentity = Enable(new EntityIdentityFeature("alert", "remove"));
        _entityOwner = Enable(new EntityOwnerFeature("alert", "remove", "removed", _entityIdentity));
        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_entityIdentity.Title == null && _entityIdentity.Id == null)
        {
            Log.Error("A `title` or `id` must be specified");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var toRemove = _entityIdentity.Id != null ? [await connection.Alerts.FindAsync(_entityIdentity.Id)]
            :
            (await connection.Alerts.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(alert => _entityIdentity.Title == alert.Title)
            .ToArray();

        if (!toRemove.Any())
        {
            Log.Error("No matching alert was found");
            return 1;
        }

        foreach (var alert in toRemove)
            await connection.Alerts.RemoveAsync(alert);

        return 0;
    }
}
