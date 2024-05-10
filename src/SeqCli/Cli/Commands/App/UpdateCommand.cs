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
using System.Globalization;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.App;

[Command("app", "update", "Update an installed app package",
    Example = "seqcli app update -n 'HTML Email'")]
// ReSharper disable once UnusedType.Global
class UpdateCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    string? _id, _name, _version;
    bool _all, _force;

    public UpdateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "i=|id=",
            "The id of a single installed app to update",
            id => _id = ArgumentString.Normalize(id));

        Options.Add(
            "n=|name=",
            "The name of the installed app to update",
            name => _name = ArgumentString.Normalize(name));

        Options.Add(
            "all",
            "Update all installed apps; not compatible with `-i` or `-n`",
            _ => _all = true);

        Options.Add(
            "version=",
            "The package version to update to; the default is to update to the latest version in the associated feed",
            version => _version = ArgumentString.Normalize(version));
        

        Options.Add(
            "force",
            "Update the app even if the target version is already installed",
            _ => _force = true);
        
        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        if (_all && (_id != null || _name != null) ||
            _id != null && _name != null)
        {
            Log.Error("The `id`, `name`, and `all` options are mutually exclusive");
            return 1;
        }
        
        if (_all && _version != null)
        {
            Log.Error("The `all` and `version` options are incompatible");
            return 1;
        }
        
        if (!_all && _id == null && _name == null)
        {
            Log.Error("One of `id`, `name`, or `all` must be specified");
            return 1;
        }
        
        var connection = _connectionFactory.Connect(_connection);

        var apps = await connection.Apps.ListAsync();
        foreach (var app in apps)
        {
            if (_all || app.Id == _id || _name != null && _name.Equals(app.Name, StringComparison.OrdinalIgnoreCase))
            {
                var updated = await connection.Apps.UpdatePackageAsync(app, _version, _force);
                _output.WriteEntity(updated);
            }
        }

        return 0;
    }
}
