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
using Seq.Api.Model.Shared;
using Seq.Api.Model.Signals;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Signals;
using SeqCli.Syntax;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.RetentionPolicy;

[Command("retention", "create", "Create a retention policy",
    Example = "seqcli retention create --after 30d --data-source stream --delete-all")]
class CreateCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    string? _afterDuration;
    bool _deleteAll;
    string? _dataSource;
    string? _deleteMatchingSignal;

    public CreateCommand()
    {
        Options.Add(
            "after=",
            "A duration after which the policy will delete events, e.g. `7d`",
            v => _afterDuration = ArgumentString.Normalize(v));

        Options.Add(
            "data-source=",
            "The data source to delete records from (`stream` for log events and traces, or `series` for metrics); defaults to `stream`",
            v => _dataSource = ArgumentString.Normalize(v));

        Options.Add(
            "delete-all",
            "The policy should delete all records in the target data source after the specified duration",
            _ => _deleteAll = true);
        
        Options.Add(
            "delete=",
            "A signal expression identifying events that should be deleted; not supported by the `series` data source",
            s =>
            {
                _deleteMatchingSignal = s;
            }
        );
        
        // Maintained for compatibility in Seq 2026.x; intended to be retired in 2027.
        Options.Add(
            "delete-all-events",
            "The policy should delete all events from `stream`",
            _ =>
            {
                _deleteAll = true;
                _dataSource = null;
            }, hidden: true);    
        
        _connection = Enable<ConnectionFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        SignalExpressionPart? removedSignalExpression;
        
        // Exactly one of `delete-all-events` or `delete` must be specified
        if (_deleteAll)
        {
            if (!string.IsNullOrEmpty(_deleteMatchingSignal))
            {
                Log.Error("Only one of the `--delete-all` or `--delete` options may be specified");
                return 1;
            }

            removedSignalExpression = null;
        }
        else if (string.IsNullOrEmpty(_deleteMatchingSignal))
        {
            Log.Error("One of either the `--delete-all` or `--delete` options must be specified");
            return 1;
        }
        else
        {
            removedSignalExpression = SignalExpressionParser.ParseExpression(_deleteMatchingSignal!);
        }
        
        if (_afterDuration == null)
        {
            Log.Error("A duration must be specified using `--after`");
            return 1;
        }

        var duration = DurationMoniker.ToTimeSpan(_afterDuration);

        if (_dataSource == null)
        {
            Log.Error("Use `--data-source` to specify `stream` or `series` as the retention target");
            return 1;
        }
        
        if (!Enum.TryParse(_dataSource, ignoreCase: true, out DataSource dataSource))
        {
            Log.Error("The `--data-source` option supports `stream` and `series`");
            return 1;
        }

        if (removedSignalExpression != null && dataSource != DataSource.Stream)
        {
            Log.Error("The `--delete` option is only valid when `--data-source` is `stream`");
            return 1;
        }
        
        var policy = await connection.RetentionPolicies.TemplateAsync();
        policy.RetentionTime = duration;
        policy.RemovedSignalExpression = removedSignalExpression;
        policy.DataSource = dataSource;

        policy = await connection.RetentionPolicies.AddAsync(policy);

        _output.GetOutputFormat(config).WriteEntity(policy);

        return 0;
    }
}