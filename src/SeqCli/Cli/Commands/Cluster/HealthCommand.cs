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
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Util;
using Seq.Api.Model.Cluster;
using SeqCli.Api;
using Serilog;

namespace SeqCli.Cli.Commands.Cluster;

[Command("cluster", "health",
    "Probe a Seq node's `/health/cluster` endpoint, and print the returned status. This command can also be used " +
    "to wait on a timeout until the cluster is healthy.",
    Example = "seqcli cluster health -s https://seq.example.com --wait-until-healthy")]
class HealthCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly TimeoutFeature _timeout;
    readonly WaitUntilHealthyFeature _waitUntilHealthy;
    readonly StoragePathFeature _storagePath;
    
    public HealthCommand()
    {
        _waitUntilHealthy = Enable(new WaitUntilHealthyFeature("cluster"));
        _timeout = Enable(new TimeoutFeature());
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
        _connection = Enable<ConnectionFeature>();
    }
        
    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var timeout = _timeout.ApplyTimeout(connection.Client.HttpClient);

        if (_waitUntilHealthy.ShouldWait)
        {
            return await RunUntilHealthy(connection, timeout ?? TimeSpan.FromSeconds(30), _output.GetOutputFormat(config));
        }

        return await RunOnce(connection, _output.GetOutputFormat(config));
    }

    async Task<int> RunUntilHealthy(SeqConnection connection, TimeSpan timeout, OutputFormat outputFormat)
    {
        using var ct = new CancellationTokenSource(timeout);
        
        var tick = TimeSpan.FromSeconds(1);

        connection.Client.HttpClient.Timeout = tick;

        try
        {
            return await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (await RunOnce(connection, outputFormat) == 0)
                        {
                            return 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("{UnhandledExceptionMessage}", Presentation.FormattedMessage(ex));
                    }

                    await Task.Delay(tick, ct.Token);
                }
            }, ct.Token);
        }
        catch (TaskCanceledException)
        {
            return 1;
        }
    }

    static async Task<int> RunOnce(SeqConnection connection, OutputFormat output)
    {
        var health = await connection.Cluster.CheckHealthAsync();

        if (output.Json)
        {
            output.WriteObject(health);
        } else if (!string.IsNullOrWhiteSpace(health.Description)) {
            Console.WriteLine($"{health.Status}: {health.Description}");
        } else {
            Console.WriteLine($"{health.Status}");
        }

        return health.Status switch
        {
            HealthStatus.Healthy => 0,
            HealthStatus.Degraded => 101,
            HealthStatus.Unhealthy => 102,
            _ => 103
        };
    }
}
