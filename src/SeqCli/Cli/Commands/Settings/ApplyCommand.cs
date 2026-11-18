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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Seq.Api.Model.Settings;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Settings;

[Command("setting", "apply", "Change a batch of runtime-configurable server settings together",
    Example = "seqcli setting apply -p InstanceTitle=Production -f OpenIdConnectClientSecret=secret.txt")]
class ApplyCommand: Command
{
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;

    readonly Dictionary<string, object> _settings = new();
    readonly List<(string Name, string? Filename)> _fileSettings = new();
    readonly List<string> _stdinSettingNames = new();

    public ApplyCommand()
    {
        Options.Add(
            "p={=}|property={=}",
            "Specify setting names and values, e.g. `-p InstanceTitle=Production -p BackupsToKeep=3`; an empty value clears the setting",
            (n, v) => SetValue(n, v));

        Options.Add(
            "f={=}|from-file={=}",
            "Read setting values from UTF-8 text files, e.g. `-f OpenIdConnectClientSecret=secret.txt`",
            (n, v) => _fileSettings.Add((n, ArgumentString.Normalize(v))));

        Options.Add(
            "from-stdin=",
            "Read the value of the named setting from `STDIN`, e.g. `--from-stdin=OpenIdConnectClientSecret`",
            n => _stdinSettingNames.Add(n));

        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        if (_stdinSettingNames.Count > 1)
        {
            Log.Error("Only one setting can be read from `STDIN`.");
            return 1;
        }

        foreach (var (name, filename) in _fileSettings)
        {
            if (filename == null || !File.Exists(filename))
            {
                Log.Error("The file `{Filename}` specified for setting {SettingName} does not exist", filename, name);
                return 1;
            }

            SetValue(name, (await File.ReadAllTextAsync(filename, Encoding.UTF8)).TrimEnd('\r', '\n'));
        }

        if (_stdinSettingNames.Count == 1)
        {
            SetValue(_stdinSettingNames[0], (await Console.In.ReadToEndAsync()).TrimEnd('\r', '\n'));
        }

        if (_settings.Count == 0)
        {
            Log.Error("At least one setting must be supplied with `-p NAME=VALUE`, `-f NAME=FILENAME`, or `--from-stdin=NAME`.");
            return 1;
        }

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        await connection.Settings.ApplyAsync(new SettingsValuesPart { Settings = _settings });

        return 0;
    }

    void SetValue(string name, string? value)
    {
        // Values aren't trimmed; some settings might include leading/trailing whitespace. They're
        // sent as strings, as in `setting set`, and converted to the setting's type by the server.
        _settings[name.Trim()] = string.IsNullOrEmpty(value) ? null! : value;
    }
}
