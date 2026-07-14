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

namespace SeqCli.Cli.Alerts;

/// <summary>
/// Shared implementation for the `alert enable` and `alert disable` commands, which select
/// alerts using the same `title`/`id` identity convention as `alert remove` and flip their
/// disabled state.
/// </summary>
abstract class SetEnabledCommand : Command
{
    readonly bool _disable;
    readonly EntityIdentityFeature _entityIdentity;
    readonly EntityOwnerFeature _entityOwner;
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;

    protected SetEnabledCommand(bool disable)
    {
        _disable = disable;
        var verb = disable ? "disable" : "enable";
        var pastParticiple = disable ? "disabled" : "enabled";
        _entityIdentity = Enable(new EntityIdentityFeature("alert", verb));
        _entityOwner = Enable(new EntityOwnerFeature("alert", verb, pastParticiple, _entityIdentity));
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

        var toUpdate = _entityIdentity.Id != null ? [await connection.Alerts.FindAsync(_entityIdentity.Id)]
            :
            (await connection.Alerts.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(alert => _entityIdentity.Title == alert.Title)
            .ToArray();

        if (!toUpdate.Any())
        {
            Log.Error("No matching alert was found");
            return 1;
        }

        foreach (var alert in toUpdate.Where(alert => alert.IsDisabled != _disable))
        {
            alert.IsDisabled = _disable;
            await connection.Alerts.UpdateAsync(alert);
        }

        return 0;
    }
}
