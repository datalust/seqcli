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
using Seq.Api;
using Seq.Api.Model.LogEvents;
using Seq.Api.Model.Security;
using Seq.Api.Model.Shared;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Levels;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.ApiKey;

[Command("apikey", "create", "Create an API key for automation or ingestion",
    Example = "seqcli apikey create -t 'Test API Key' -p Environment=Test")]
class CreateCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly PropertiesFeature _properties;
    readonly OutputFormatFeature _output;

    string? _title, _token, _filter, _level, _connectUsername, _connectPassword;
    string[]? _permissions;
    bool _useServerTimestamps, _connectPasswordStdin;

    public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "t=|title=",
            "A title for the API key",
            t => _title = ArgumentString.Normalize(t));

        Options.Add(
            "token=",
            "A pre-allocated API key token; by default, a new token will be generated and written to `STDOUT`",
            t => _token = ArgumentString.Normalize(t));

        _properties = Enable<PropertiesFeature>();

        Options.Add(
            "filter=",
            "A filter to apply to incoming events",
            f => _filter = ArgumentString.Normalize(f));

        Options.Add(
            "minimum-level=",
            "The minimum event level/severity to accept; the default is to accept all events",
            v => _level = ArgumentString.Normalize(v));

        Options.Add(
            "use-server-timestamps",
            "Discard client-supplied timestamps and use server clock values",
            _ => _useServerTimestamps = true);

        Options.Add(
            "permissions=",
            "A comma-separated list of permissions to delegate to the API key; valid permissions are `Ingest` (default), `Read`, `Write`, `Project`, `Organization`, and `System`",
            v => _permissions = ArgumentString.NormalizeList(v));

        Options.Add(
            "connect-username=",
            "A username to connect with, useful primarily when setting up the first API key; servers with an 'Individual' subscription only allow one simultaneous request with this option",
            v => _connectUsername = ArgumentString.Normalize(v));

        Options.Add(
            "connect-password=",
            "When `connect-username` is specified, a corresponding password",
            v => _connectPassword = ArgumentString.Normalize(v));

        Options.Add(
            "connect-password-stdin",
            "When `connect-username` is specified, read the corresponding password from `STDIN`",
            _ => _connectPasswordStdin = true);

        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        var connection = await TryConnectAsync();
        if (connection == null)
            return 1;
            
        var apiKey = await connection.ApiKeys.TemplateAsync();

        apiKey.Title = _title;
        apiKey.InputSettings.AppliedProperties = _properties.FlatProperties
            .Select(kvp => new EventPropertyPart(kvp.Key, kvp.Value))
            .ToList();
        apiKey.InputSettings.UseServerTimestamps = _useServerTimestamps;

        // If _token is null, a value will be returned when the key is created
        apiKey.Token = _token;

        if (_filter != null)
        {
            apiKey.InputSettings.Filter = new DescriptiveFilterPart
            {
                Filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression,
                FilterNonStrict = _filter
            };
        }

        if (_level != null)
        {
            apiKey.InputSettings.MinimumLevel = Enum.Parse<LogEventLevel>(LevelMapping.ToFullLevelName(_level));
        }

        apiKey.AssignedPermissions.Clear();
        if (_permissions != null)
        {
            foreach (var permission in _permissions)
            {
                if (!Enum.TryParse<Permission>(permission, true, out var p))
                {
                    Log.Error("Unrecognized permission {Permission}", permission);
                    return 1;
                }

                apiKey.AssignedPermissions.Add(p);
            }
        }
        else
        {
            apiKey.AssignedPermissions.Add(Permission.Ingest);
        }

        apiKey = await connection.ApiKeys.AddAsync(apiKey);

        if (_token == null && !_output.Json)
        {
            Console.WriteLine(apiKey.Token);
        }
        else
        {
            _output.WriteEntity(apiKey);
        }

        return 0;
    }

    async Task<SeqConnection?> TryConnectAsync()
    {
        SeqConnection connection;
        if (_connectUsername != null)
        {
            if (_connection.IsApiKeySpecified)
            {
                Log.Error("The `connect-username` and `apikey` options are mutually exclusive");
                return null;
            }
                
            if (_connectPasswordStdin)
            {
                if (_connectPassword != null)
                {
                    Log.Error("The `connect-password` and `connect-password-stdin` options are mutually exclusive");
                    return null;
                }

                _connectPassword = await Console.In.ReadLineAsync();
            }

            var (url, _) = _connectionFactory.GetConnectionDetails(_connection);
            connection = new SeqConnection(url);
            await connection.Users.LoginAsync(_connectUsername, _connectPassword ?? "");
        }
        else
        {
            connection = _connectionFactory.Connect(_connection);
        }

        return connection;
    }
}