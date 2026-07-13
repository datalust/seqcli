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
using System.Threading;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands;

[Command("tail", "Stream log events matching a filter")]
// ReSharper disable once UnusedType.Global
class TailCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly SignalExpressionFeature _signal;
    readonly StoragePathFeature _storagePath;
    string? _filter;

    public TailCommand()
    {
        Options.Add(
            "f=|filter=",
            "An optional server-side filter to apply to the stream, for example `@Level = 'Error'`",
            v => _filter = v);

        _output = Enable(new OutputFormatFeature(supportNative: true));
        _storagePath = Enable<StoragePathFeature>();
        _signal = Enable<SignalExpressionFeature>();
        _connection = Enable<ConnectionFeature>();
    }

    protected override async Task<int> Run()
    {
        var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_,_) => cancel.Cancel();

        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        string? strict = null;
        if (!string.IsNullOrWhiteSpace(_filter))
        {
            var converted = await connection.Expressions.ToStrictAsync(_filter, cancel.Token);
            strict = converted.StrictExpression;
        }
        
        var output = _output.GetOutputFormat(config);
        
        try
        {
            await foreach (var evt in connection.Events.StreamAsync(
                               filter: strict,
                               signal: _signal.Signal,
                               render: true,
                               cancellationToken: cancel.Token))
            {
                output.WriteEventEntity(evt);
            }
        }
        catch (OperationCanceledException)
        {
            // All done!
        }
        
        return 0;
    }
}