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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api.Model.Shared;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.View;

[Command("view", "create", "Create a metrics view",
    Example = "seqcli view create -t 'API' -g '@Scope.name' -f \"@Resource.service.name = 'seq_cafe_web'\"")]
class CreateCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    string? _title, _description, _filter;
    readonly List<string> _groups = [];
    bool _isProtected;

    public CreateCommand()
    {
        Options.Add(
            "t=|title=",
            "A title for the view",
            t => _title = ArgumentString.Normalize(t));

        Options.Add(
            "description=",
            "A description for the view",
            d => _description = ArgumentString.Normalize(d));

        Options.Add(
            "f=|filter=",
            "Expression filter to associate with the view",
            f => _filter = ArgumentString.Normalize(f));

        Options.Add(
            "g=|group=",
            "Group key expression to associate with the view; this argument can be used multiple times",
            c => _groups.Add(ArgumentString.Normalize(c) ?? throw new ArgumentException("Group keys require a value.")));

        Options.Add(
            "protected",
            "Specify that the view is editable only by administrators",
            _ => _isProtected = true);

        _connection = Enable<ConnectionFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);
        
        if (string.IsNullOrEmpty(_title))
        {
            Log.Error("A title must be specified");
            return 1;
        }

        var view = await connection.Views.TemplateAsync();
        view.OwnerId = null;

        view.Title = _title;
        view.Description = _description;
        view.IsProtected = _isProtected;

        if (_filter != null)
        {
            view.Filters = new[]
            {
                new DescriptiveFilterPart
                {
                    Filter = (await connection.Expressions.ToStrictAsync(_filter)).StrictExpression,
                    FilterNonStrict = _filter
                }
            }.ToList();
        }

        foreach (var group in _groups)
            view.GroupKeyExpressions.Add(group);

        view = await connection.Views.AddAsync(view);

        _output.GetOutputFormat(config).WriteEntity(view);

        return 0;
    }
}