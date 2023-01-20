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
using SeqCli.Syntax;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.RetentionPolicy;

[Command("retention", "create", "Create a retention policy",
    Example = "seqcli retention create --after 30d --delete-all-events")]
class CreateCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;

    string? _afterDuration;
    bool _deleteAllEvents; // Currently the only supported option

    public CreateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

        Options.Add(
            "after=",
            "A duration after which the policy will delete events, e.g. `7d`",
            v => _afterDuration = ArgumentString.Normalize(v));
            
        Options.Add(
            "delete-all-events",
            "The policy should delete all events (currently the only supported option)",
            _ => _deleteAllEvents = true);

        _connection = Enable<ConnectionFeature>();
        _output = Enable(new OutputFormatFeature(config.Output));
    }

    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        if (!_deleteAllEvents)
        {
            Log.Error("The `delete-all-events` option must be specified");
            return 1;
        }
            
        if (_afterDuration == null)
        {
            Log.Error("A duration must be specified using `after`");
            return 1;
        }

        var duration = DurationMoniker.ToTimeSpan(_afterDuration);
            
        var policy = await connection.RetentionPolicies.TemplateAsync();
        policy.RetentionTime = duration;

        policy = await connection.RetentionPolicies.AddAsync(policy);

        _output.WriteEntity(policy);

        return 0;
    }
}