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

using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.View;

[Command("view", "list", "List available metrics views", Example="seqcli view list")]
class ListCommand : Command
{
    readonly EntityIdentityFeature _entityIdentity;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly EntityOwnerFeature _entityOwner;
    readonly StoragePathFeature _storagePath;
    
    public ListCommand()
    {
        _entityIdentity = Enable(new EntityIdentityFeature("view", "list"));
        _entityOwner = Enable(new EntityOwnerFeature("view", "list", "listed", _entityIdentity));
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var list = _entityIdentity.Id != null ? [await connection.Views.FindAsync(_entityIdentity.Id)]
            :
            (await connection.Views.ListAsync(ownerId: _entityOwner.OwnerId, shared: _entityOwner.IncludeShared))
            .Where(signal => _entityIdentity.Title == null || _entityIdentity.Title == signal.Title);

        _output.GetOutputFormat(config).ListEntities(list);

        return 0;
    }
}