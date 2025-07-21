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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Cli.Commands.Node;

[Command("node", "health",
    "Probe a Seq node's `/health` endpoint, and print the returned HTTP status code, or 'Unreachable' if the endpoint could not be queried; note that no API key is required",
    Example = "seqcli node health -s https://seq-2.example.com")]
class HealthCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly WaitUntilHealthyFeature _waitUntilHealthy;
    readonly TimeoutFeature _timeout;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storagePath;
    
    public HealthCommand()
    {

        _waitUntilHealthy = Enable(new WaitUntilHealthyFeature("node"));
        _timeout = Enable(new TimeoutFeature());
        _connection = Enable<ConnectionFeature>();
        _output = Enable<OutputFormatFeature>();
        _storagePath = Enable<StoragePathFeature>();
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
                    if (await RunOnce(connection, outputFormat) == 0)
                    {
                        return 0;
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

    async Task<int> RunOnce(SeqConnection connection, OutputFormat outputFormat)
    {
        try
        {
            var response = await connection.Client.HttpClient.GetAsync("health");
            Log.Information("HTTP {HttpVersion} {StatusCode} {ReasonPhrase}", response.Version, (int)response.StatusCode, response.ReasonPhrase);
            
            foreach (var (key, values) in response.Headers.Concat(response.Content.Headers))
            foreach (var value in values)
            {
                Log.Information("{HeaderName}: {HeaderValue}", key, value);
            }

            if (outputFormat.Json)
            {
                var shouldBeJson = await response.Content.ReadAsStringAsync();
                try
                {
                    var obj = JsonConvert.DeserializeObject(shouldBeJson) ?? throw new InvalidDataException();
                    outputFormat.WriteObject(obj);
                }
                catch
                {
                    outputFormat.WriteObject(new { Response = shouldBeJson });
                }
            }
            else
            {
                Console.WriteLine((int)response.StatusCode);
            }
            
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Exception thrown when calling health endpoint");

            Console.WriteLine("Unreachable");
            return 1;
        }
    }
}