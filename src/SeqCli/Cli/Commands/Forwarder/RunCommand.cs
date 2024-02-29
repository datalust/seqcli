// Copyright © Datalust Pty Ltd
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Config.Forwarder;
using SeqCli.Forwarder;
using SeqCli.Forwarder.Util;
using SeqCli.Forwarder.Web.Host;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

// ReSharper disable UnusedType.Global

namespace SeqCli.Cli.Commands.Forwarder;

[Command("forwarder", "run", "Run the server interactively")]
class RunCommand : Command
{
    readonly StoragePathFeature _storagePath;
    readonly ListenUriFeature _listenUri;

    bool _noLogo;

    public RunCommand()
    {
        Options.Add("nologo", _ => _noLogo = true);
        _storagePath = Enable<StoragePathFeature>();
        _listenUri = Enable<ListenUriFeature>();
    }

    protected override async Task<int> Run(string[] unrecognized)
    {
        if (Environment.UserInteractive)
        {
            if (!_noLogo)
            {
                WriteBanner();
                Console.WriteLine();
            }

            Console.WriteLine("Running as server; press Ctrl+C to exit.");
            Console.WriteLine();
        }

        SeqCliConfig config;

        try
        {
            config = SeqCliConfig.Read(); // _storagePath.ConfigFilePath);
        }
        catch (Exception ex)
        {
            await using var logger = CreateLogger(
                LogEventLevel.Information,
                ForwarderDiagnosticConfig.GetDefaultInternalLogPath());

            logger.Fatal(ex, "Failed to load configuration from {ConfigFilePath}", _storagePath.ConfigFilePath);
            return 1;
        }

        Log.Logger = CreateLogger(
            config.Forwarder.Diagnostics.InternalLoggingLevel,
            config.Forwarder.Diagnostics.InternalLogPath,
            config.Forwarder.Diagnostics.InternalLogServerUri,
            config.Forwarder.Diagnostics.InternalLogServerApiKey);

        var listenUri = _listenUri.ListenUri ?? config.Forwarder.Api.ListenUri;

        try
        {
            ILifetimeScope? container = null;
            using var host = new HostBuilder()
                .UseSerilog()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.RegisterBuildCallback(ls => container = ls);
                    builder.RegisterModule(new ForwarderModule(_storagePath.BufferPath, config));
                })
                .ConfigureWebHostDefaults(web =>
                {
                    web.UseStartup<Startup>();
                    web.UseKestrel(options =>
                        {
                            options.AddServerHeader = false;
                            options.AllowSynchronousIO = true;
                        })
                        .ConfigureKestrel(options =>
                        {
                            var apiListenUri = new Uri(listenUri);

                            var ipAddress = apiListenUri.HostNameType switch
                            {
                                UriHostNameType.Basic => IPAddress.Any,
                                UriHostNameType.Dns => IPAddress.Any,
                                UriHostNameType.IPv4 => IPAddress.Parse(apiListenUri.Host),
                                UriHostNameType.IPv6 => IPAddress.Parse(apiListenUri.Host),
                                _ => throw new NotSupportedException($"Listen URI type `{apiListenUri.HostNameType}` is not supported.")
                            };
                            
                            if (apiListenUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                            {
                                options.Listen(ipAddress, apiListenUri.Port, listenOptions =>
                                {
#if WINDOWS
                                    listenOptions.UseHttps(StoreName.My, apiListenUri.Host,
                                        location: StoreLocation.LocalMachine, allowInvalid: true);
#else
                                    listenOptions.UseHttps();
#endif
                                });
                            }
                            else
                            {
                                options.Listen(ipAddress, apiListenUri.Port);
                            }
                        });
                })
                .Build();

            if (container == null) throw new Exception("Host did not build container.");
                
            var service = container.Resolve<ServerService>(
                new TypedParameter(typeof(IHost), host),
                new NamedParameter("listenUri", listenUri));
                
            var exit = ExecutionEnvironment.SupportsStandardIO
                ? RunStandardIO(service, Console.Out)
                : RunService(service);

            return exit;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception");
            return -1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    static int RunService(ServerService service)
    {
#if WINDOWS
            System.ServiceProcess.ServiceBase.Run([
                new ServiceProcess.SeqForwarderWindowsService(service)
            ]);
            return 0;
#else
        throw new NotSupportedException("Windows services are not supported on this platform.");            
#endif
    }
        
    static int RunStandardIO(ServerService service, TextWriter cout)
    {
        service.Start();

        try
        {
            Console.TreatControlCAsInput = true;
            var k = Console.ReadKey(true);
            while (k.Key != ConsoleKey.C || !k.Modifiers.HasFlag(ConsoleModifiers.Control))
                k = Console.ReadKey(true);

            cout.WriteLine("Ctrl+C pressed; stopping...");
            Console.TreatControlCAsInput = false;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Console not attached, waiting for any input");
            Console.Read();
        }

        service.Stop();

        return 0;
    }

    static void WriteBanner()
    {
        Write("─", ConsoleColor.DarkGray, 47);
        Console.WriteLine();
        Write(" Seq Forwarder", ConsoleColor.White);
        Write(" ──", ConsoleColor.DarkGray);
        Write(" © 2024 Datalust Pty Ltd", ConsoleColor.Gray);
        Console.WriteLine();
        Write("─", ConsoleColor.DarkGray, 47);
        Console.WriteLine();
    }

    static void Write(string s, ConsoleColor color, int repeats = 1)
    {
        Console.ForegroundColor = color;
        for (var i = 0; i < repeats; ++i)
            Console.Write(s);
        Console.ResetColor();
    }
    
    static Logger CreateLogger(
        LogEventLevel internalLoggingLevel,
        string internalLogPath,
        string? internalLogServerUri = null,
        string? internalLogServerApiKey = null)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("Application", "Seq Forwarder")
            .MinimumLevel.Is(internalLoggingLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.File(
                new RenderedCompactJsonFormatter(),
                GetRollingLogFilePathFormat(internalLogPath),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 1024 * 1024);

        if (Environment.UserInteractive)
            loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);

        if (!string.IsNullOrWhiteSpace(internalLogServerUri))
            loggerConfiguration.WriteTo.Seq(
                internalLogServerUri,
                apiKey: internalLogServerApiKey);

        return loggerConfiguration.CreateLogger();
    }

    static string GetRollingLogFilePathFormat(string internalLogPath)
    {
        if (internalLogPath == null) throw new ArgumentNullException(nameof(internalLogPath));

        return Path.Combine(internalLogPath, "seq-forwarder-.log");
    }
}