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
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Mcp;
using SeqCli.Mcp.Tools.Search;
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
        Options.Add("debug", "Write diagnostic messages from the MCP server back through the connection.",
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
            var builder = Host.CreateApplicationBuilder();
            builder.ConfigureContainer(new AutofacServiceProviderFactory());
            builder.Services.AddSerilog();
            builder.Services.AddSingleton(_ => SeqConnectionFactory.Connect(_connection, config));
            builder.Services.AddSingleton<McpSession>();
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithTools([
                    typeof(SearchAndQueryToolType)
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
}