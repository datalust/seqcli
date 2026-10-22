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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Protocol;
using Seq.Api;
using Seq.Api.Model.Settings;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Mcp;
using SeqCli.Mcp.Tools.Metrics;
using SeqCli.Mcp.Tools.Query;
using SeqCli.Mcp.Tools.Search;
using SeqCli.Mcp.Tools.Signals;
using SeqCli.Mcp.Tools.Traces;
using Serilog;

namespace SeqCli.Cli.Commands.Mcp;

[Command("mcp", "run", "Run an MCP (Model Context Protocol) server on STDIO")]
class RunCommand: Command
{
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;
    bool _debug;

    public RunCommand()
    {
        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
        Options.Add("debug", "Write diagnostic messages from the MCP server back through the connection",
            _ => _debug = true);
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);

        if (_debug)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("Application", "seqcli mcp run")
                .WriteTo.Seq(config.Connection.ServerUrl, apiKey: config.Connection.DecodeApiKey(config.Encryption.DataProtector()))
                .CreateLogger();
            
            Log.Information("Seq MCP server starting up");
        }

        try
        {
            // Avoid creating masses of (inefficient) filesystem watchers on the CWD.
            Environment.SetEnvironmentVariable("DOTNET_hostBuilder:reloadConfigOnChange", "false");

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true });
            builder.Services.AddSerilog();
            var connection = SeqConnectionFactory.Connect(_connection, config);
            var instanceTitle = await TryGetInstanceTitleAsync(connection);

            builder.Services.AddSingleton(connection);
            builder.Services.AddSingleton<McpSession>();
            builder.Services
                .AddMcpServer(options =>
                {
                    var identity = instanceTitle != null ? $"Seq ({instanceTitle})" : "Seq";
                    var assemblyName = Assembly.GetEntryAssembly()?.GetName();
                    options.ServerInfo = new Implementation
                    {
                        Name = identity,
                        Version = assemblyName?.Version?.ToString() ?? "0.0.0"
                    };

                    if (instanceTitle != null)
                        options.ServerInstructions = $"Connected to {identity}";
                })
                .WithStdioServerTransport()
                .WithTools([
                    typeof(SearchTools),
                    typeof(MetricsTools),
                    typeof(QueryTools),
                    typeof(SignalTools),
                    typeof(TraceTools)
                ]);

            await builder.Build().RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
        return 0;
    }

    static async Task<string?> TryGetInstanceTitleAsync(SeqConnection connection)
    {
        try
        {
            // Not really any good options, here - we might have a 1-2 second deadline for first-turn availability,
            // and 30-60 second absolute deadline. It's likely the call will be subsecond, but if we miss the first
            // turn, might as well keep waiting, the tool won't work if Seq is down, anyway.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var setting = await connection.Settings.FindNamedAsync(SettingName.InstanceTitle, cts.Token);
            return setting.Value is string title && !string.IsNullOrWhiteSpace(title) ? title.Trim() : null;
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Could not retrieve the Seq instance title");
            return null;
        }
    }
}