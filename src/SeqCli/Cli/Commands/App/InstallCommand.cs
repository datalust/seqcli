// Copyright Datalust Pty Ltd and Contributors
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
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

#nullable enable

namespace SeqCli.Cli.Commands.App;

[Command("app", "install", "Install an app package",
    Example = "seqcli app install --package-id 'Seq.App.JsonArchive'")]
// ReSharper disable once UnusedType.Global
class InstallCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    string? _packageId, _version, _feedId;

    public InstallCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "package-id=",
            "The package id of the app to install",
            packageId => _packageId = ArgumentString.Normalize(packageId));

        Options.Add(
            "version=",
            "The package version to install; the default is to install the latest version",
            version => _version = ArgumentString.Normalize(version));

        Options.Add(
            "feed-id=",
            "The id of the NuGet feed to install the package from; may be omitted if only one feed is configured",
            feedId => _feedId = ArgumentString.Normalize(feedId));
        
        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        if (_packageId == null)
        {
            Log.Error("A `package-id` is required");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);

        var feedId = _feedId;
        if (feedId == null)
        {
            var feeds = await connection.Feeds.ListAsync();
            if (feeds.Count != 1)
            {
                Log.Error("A `feed-id` is required unless the server has exactly one configured feed");
                return 1;
            }

            feedId = feeds.Single().Id;
        }

        var app = await connection.Apps.InstallPackageAsync(feedId, _packageId, _version);
        _output.WriteEntity(app);

        return 0;
    }
}
