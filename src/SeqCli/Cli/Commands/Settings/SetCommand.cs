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
using SeqCli.Config;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands.Settings;

[Command("setting", "set", "Change a runtime-configurable server setting")]
class SetCommand: Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly SettingNameFeature _name;
    readonly StoragePathFeature _storagePath;
    
    string? _value;
    bool _valueSpecified, _readValueFromStdin;

    public SetCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        _name = Enable<SettingNameFeature>();
        
        Options.Add("v|value=",
            "The setting value, comma-separated if multiple values are accepted",
            v =>
            {
                // Not normalized; some settings might include leading/trailing whitespace.
                _valueSpecified = true;
                _value = v;
            });

        Options.Add("value-stdin",
            "Read the value from `STDIN`",
            _ => _readValueFromStdin = true);

        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        if (!_valueSpecified && !_readValueFromStdin)
        {
            Log.Error("A value must be supplied with either `--value=VALUE` or `--value-stdin`.");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = _connectionFactory.Connect(_connection, config);

        var setting = await connection.Settings.FindNamedAsync(_name.Name);
        setting.Value = ReadValue();
        await connection.Settings.UpdateAsync(setting);

        return 0;
    }

    string? ReadValue()
    {
        if (_readValueFromStdin)
            return Console.In.ReadToEnd().TrimEnd('\r', '\n');

        return _value;
    }
}