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
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Settings;

[Command("setting", "clear", "Clear a runtime-configurable server setting")]
class ClearCommand: Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly SettingNameFeature _name;

    public ClearCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _name = Enable<SettingNameFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        var setting = await connection.Settings.FindNamedAsync(_name.Name);
        setting.Value = null;
        await connection.Settings.UpdateAsync(setting);

        return 0;
    }
}