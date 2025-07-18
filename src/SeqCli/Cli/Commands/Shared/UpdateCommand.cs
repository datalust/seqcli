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
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Templates.Ast;
using SeqCli.Templates.Import;
using SeqCli.Templates.Parser;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Shared;

abstract class UpdateCommand: Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    readonly string _resourceGroupName;
    readonly string _entityName;

    string? _json;
    bool _jsonStdin;

    protected UpdateCommand(SeqConnectionFactory connectionFactory, string commandGroupName, string resourceGroupName, string? entityName = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _resourceGroupName = resourceGroupName;
        _entityName = entityName ?? commandGroupName;

        Options.Add(
            "json=",
            $"The updated {_entityName} in JSON format; this can be produced using `seqcli {commandGroupName} list --json`",
            p => _json = ArgumentString.Normalize(p));
        
        Options.Add(
            "json-stdin",
            $"Read the updated {_entityName} as JSON from `STDIN`",
            _ => _jsonStdin = true);
        
        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }
    
    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = _connectionFactory.Connect(_connection, config);

        if (_json == null && !_jsonStdin)
        {
            Log.Error("One of either `--json` or `--json-stdin` must be specified");
            return 1;
        }
        
        var json = _json ?? await Console.In.ReadToEndAsync();
        
        if (!JsonTemplateParser.TryParse(json, out var template, out var error, out _))
        {
            Log.Error("The {EntityName} JSON could not be parsed: {Error}", _entityName, error);
            return 1;
        }

        if (template is not JsonTemplateObject obj ||
            !obj.Members.TryGetValue("Id", out var idValue) ||
            idValue is not JsonTemplateString id)
        {
            Log.Error("The {EntityName} JSON must be an object literal with a valid string `Id` property", _entityName);
            return 1;
        }
        
        var templateName = "JSON";
        var entityTemplate = new EntityTemplate(_resourceGroupName, templateName, template);
        var state = new TemplateImportState();
        state.AddOrUpdateCreatedEntityId(templateName, id.Value);
        await TemplateSetImporter.ImportAsync([entityTemplate], connection, new Dictionary<string, JsonTemplate>(), state, merge: false);
        
        return 0;
    }
}
