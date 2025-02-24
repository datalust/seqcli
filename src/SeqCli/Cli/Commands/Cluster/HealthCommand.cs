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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;
using SeqCli.Util;
using Seq.Api.Model.Cluster;
using Serilog;

namespace SeqCli.Cli.Commands.Cluster;

[Command("cluster", "health",
    "Probe a Seq node's `/health/cluster` endpoint, and print the returned status. This command can also be used " +
    "to wait on a timeout until the cluster is healthy.",
    Example = "seqcli cluster health -s https://seq.example.com --wait-until-healthy")]
class HealthCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    readonly ConnectionFeature _connection;
    readonly OutputFormatFeature _output;
    readonly TimeoutFeature _timeout;

    bool _waitUntilHealthy;
    
    public HealthCommand(SeqConnectionFactory connectionFactory, SeqCliOutputConfig outputConfig)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        
        Options.Add("wait-until-healthy", "Wait until the cluster returns a status of healthy", _ =>
        {
            _waitUntilHealthy = true;
        });

        _timeout = Enable(new TimeoutFeature());
        _output = Enable(new OutputFormatFeature(outputConfig));
        _connection = Enable<ConnectionFeature>();
    }
        
    protected override async Task<int> Run()
    {
        var connection = _connectionFactory.Connect(_connection);

        var timeout = _timeout.ApplyTimeout(connection.Client.HttpClient);

        if (_waitUntilHealthy)
        {
            return await RunUntilHealthy(connection, timeout ?? TimeSpan.FromSeconds(30));
        }

        return await RunOnce(connection);
    }

    async Task<int> RunUntilHealthy(SeqConnection connection, TimeSpan timeout)
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
                        if (await RunOnce(connection) == 0)
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

    async Task<int> RunOnce(SeqConnection connection)
    {
        var health = await connection.Cluster.CheckHealthAsync();

        if (_output.Json)
        {
            _output.WriteObject(health);
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
