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

namespace SeqCli.Cli.Commands.RetentionPolicy;

[Command("retention", "create", "Create a retention policy",
    Example = "seqcli retention create --after 30d --delete-all-events")]
class CreateCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    string? _afterDuration;
    bool _deleteAllEvents;
    string? _deleteMatchingSignal;

    public CreateCommand(SeqConnectionFactory connectionFactory)
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
        
        Options.Add(
            "delete=",
            "Stream incoming events to this app instance as they're ingested; optionally accepts a signal expression limiting which events should be streamed",
            s =>
            {
                _deleteMatchingSignal = s;
            }
        );

        _connection = Enable<ConnectionFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = _connectionFactory.Connect(_connection, config);

        SignalExpressionPart? removedSignalExpression;
        
        // Exactly one of `delete-all-events` or `delete` must be specified
        if (_deleteAllEvents)
        {
            if (!string.IsNullOrEmpty(_deleteMatchingSignal))
            {
                Log.Error("Only one of the `delete-all-events` or `delete` options may be specified");
                return 1;
            }

            removedSignalExpression = null;
        }
        else if (string.IsNullOrEmpty(_deleteMatchingSignal))
        {
            Log.Error("Either the `delete-all-events` or `delete` options must be specified");
            return 1;
        }
        else
        {
            removedSignalExpression = SignalExpressionParser.ParseExpression(_deleteMatchingSignal!);
        }
        
        if (_afterDuration == null)
        {
            Log.Error("A duration must be specified using `after`");
            return 1;
        }

        var duration = DurationMoniker.ToTimeSpan(_afterDuration);
            
        var policy = await connection.RetentionPolicies.TemplateAsync();
        policy.RetentionTime = duration;
        policy.RemovedSignalExpression = removedSignalExpression;

        policy = await connection.RetentionPolicies.AddAsync(policy);

        _output.GetOutputFormat(config).WriteEntity(policy);

        return 0;
    }
}