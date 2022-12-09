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
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Sample.Loader;

namespace SeqCli.Cli.Commands.Sample;

[Command("sample", "ingest", "Log sample events into a Seq instance",
    Example = "seqcli sample ingest")]
class IngestCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;
        
    readonly ConnectionFeature _connection;
    readonly ConfirmFeature _confirm;
    readonly BatchSizeFeature _batchSize;

    bool _quiet;

    public IngestCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _confirm = Enable<ConfirmFeature>();
        _connection = Enable<ConnectionFeature>();

        Options.Add("quiet", "Don't echo ingested events to `STDOUT`", _ => _quiet = true);
            
        _batchSize = Enable<BatchSizeFeature>();
    }
        
    protected override async Task<int> Run()
    {
        var (url, apiKey) = _connectionFactory.GetConnectionDetails(_connection);
        var batchSize = _batchSize.Value;
            
        if (!_confirm.TryConfirm($"This will send sample events to the Seq server at {url}."))
        {
            await Console.Error.WriteLineAsync("Canceled by user.");
            return 1;
        }

        var connection = _connectionFactory.Connect(_connection);
        await Simulation.RunAsync(connection, apiKey, batchSize, echoToStdout: !_quiet);
        return 0;
    }
}