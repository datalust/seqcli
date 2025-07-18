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
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.User;

[Command("user", "list", "List users", Example="seqcli user list")]
class ListCommand : Command
{
    readonly UserIdentityFeature _userIdentity;
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    public ListCommand()
    {
        _userIdentity = Enable(new UserIdentityFeature("list"));
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var list = _userIdentity.Id != null ? [await connection.Users.FindAsync(_userIdentity.Id)]
            :
            (await connection.Users.ListAsync())
            .Where(u => _userIdentity.Name == null || _userIdentity.Name == u.Username);

        _output.GetOutputFormat(config).ListEntities(list);
            
        return 0;
    }
}