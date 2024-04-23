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
using Seq.Api.Model.Signals;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Signals;
using SeqCli.Syntax;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.ExpressionIndex;

[Command("expressionindex", "create", "Create an expression index",
    Example = "seqcli expressionindex create --expression \"ServerName\"")]
class CreateCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    string? _expression;

    public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "expression=",
            "The expression to index",
            v => _expression = ArgumentString.Normalize(v));

        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        if (string.IsNullOrEmpty(_expression))
        {
            Log.Error("An `expression` must be specified");
            return 1;
        }

        var index = await connection.ExpressionIndexes.TemplateAsync();
        index.Expression = _expression;
        index = await connection.ExpressionIndexes.AddAsync(index);

        _output.WriteEntity(index);

        return 0;
    }
}