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
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Sample.Loader;

namespace SeqCli.Cli.Commands.Sample;

[Command("sample", "ingest", "Log sample events into a Seq instance",
    Example = "seqcli sample ingest")]
class IngestCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly ConfirmFeature _confirm;
    readonly BatchSizeFeature _batchSize;
    readonly StoragePathFeature _storagePath;
    
    bool _quiet;
    bool _setup;
    int _simulations = 1;

    public IngestCommand()
    {
        _confirm = Enable<ConfirmFeature>();
        _connection = Enable<ConnectionFeature>();

        Options.Add("quiet", "Don't echo ingested events to `STDOUT`", _ => _quiet = true);
        Options.Add("setup", "Configure sample dashboards, signals, users, and so on before starting ingestion", _ => _setup = true);
        Options.Add("simulations=", "Number of concurrent simulations to run; the default runs a single simulation",
            v => _simulations = int.Parse(v));
            
        _batchSize = Enable<BatchSizeFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }
        
    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var (url, apiKey) = SeqConnectionFactory.GetConnectionDetails(_connection, config);
        var batchSize = _batchSize.Value;

        if (!_confirm.TryConfirm(_setup
            ? $"This will apply sample configuration and send sample events to the Seq server at {url}."
            : $"This will send sample events to the Seq server at {url}."
        )) {
            await Console.Error.WriteLineAsync("Canceled by user.");
            return 1;
        }

        var connection = SeqConnectionFactory.Connect(_connection, config);

        if (_setup)
        {
            var setupResult = await SetupCommand.ImportTemplates(connection);

            if (setupResult != 0)
            {
                return setupResult;
            }
        }

        var simulations = Enumerable.Range(0, _simulations)
            .Select(_ => Simulation.RunAsync(connection, apiKey, batchSize, echoToStdout: !_quiet))
            .ToList();

        await Task.WhenAll(simulations);
        
        return 0;
    }
}