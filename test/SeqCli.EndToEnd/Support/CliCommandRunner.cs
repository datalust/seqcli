using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
// ReSharper disable MemberCanBePrivate.Global

#nullable enable

namespace SeqCli.EndToEnd.Support;

public class CliCommandRunner(TestConfiguration configuration, TestDataFolder testDataFolder)
{
    public static readonly TimeSpan DefaultExecTimeout = TimeSpan.FromSeconds(10);
        
    public ITestProcess? LastRunProcess { get; private set; }

    public int Exec(string command, string? args = null, bool disconnected = false, Dictionary<string, string>? environment = null, TimeSpan? timeout = null)
    {
        using var process = Spawn(command, args, disconnected, environment);
        return process.WaitForExit(timeout ?? DefaultExecTimeout);
    }

    public CaptiveProcess Spawn(string command, string? args = null, bool disconnected = false, Dictionary<string, string>? environment = null)
    {
        var process = configuration.SpawnCliProcess(command, args, environment, skipServerArg: disconnected);
        LastRunProcess = process;
        return process;
    }

    public async Task<(CaptiveProcess, string)> SpawnForwarderAsync(Dictionary<string, string>? environment = null)
    {
        var forwarderApiListenUri = $"http://127.0.0.1:{configuration.AllocatePort()}";

        var env = environment ?? new();
        env.Add("SEQCLI_FORWARDER_API_LISTENURI", forwarderApiListenUri);

        var forwarder = Spawn("forwarder run", $"--pre --storage=\"{testDataFolder.Path}\"", environment: env);

        await WaitForForwarderConnectionAsync(forwarderApiListenUri);

        return (forwarder, forwarderApiListenUri);
    }
    
    
    static async Task WaitForForwarderConnectionAsync(string forwarderApiListenUri)
    {
        using var httpClient = new HttpClient();
        var ingestEndpoint = $"{forwarderApiListenUri}/ingest/clef";
        var content = new StringContent("", new MediaTypeHeaderValue("application/vnd.serilog.clef", "utf-8"));
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        
        while (true)
        {
            try
            {
                var ingestionResult = await httpClient.PostAsync(ingestEndpoint, content, cts.Token);
                if (ingestionResult.IsSuccessStatusCode)
                    return;
                Log.Information("Waiting for forwarder API to become available; last result {StatusCode}", ingestionResult.StatusCode);
            }
            catch (Exception ex)
            {
                // Back around the loop
                Log.Information("Waiting for forwarder API to become available; the last request failed ({Message})", ex.Message);
            }

            await Task.Delay(1000, cts.Token);
        }
    }
}
