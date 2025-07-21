using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Forwarder;

public class ForwarderIngestionLogTestCase(TestConfiguration testConfiguration): ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var forwarderApiListenUri = $"http://0.0.0.0:{testConfiguration.AllocatePort()}";

        using (testConfiguration.SpawnCliProcess("forwarder run", environment: new()
        {
            ["SEQCLI_FORWARDER_API_LISTENURI"] = forwarderApiListenUri
        }))
        {
            await WaitForForwarderConnectionAsync(forwarderApiListenUri);

            var exit = runner.Exec($"diagnostics ingestionlog -s {forwarderApiListenUri}");
            Assert.NotEqual(0, exit);

        }

        forwarderApiListenUri = $"http://0.0.0.0:{testConfiguration.AllocatePort()}";

        using (testConfiguration.SpawnCliProcess("forwarder run", environment: new()
               {
                   ["SEQCLI_FORWARDER_API_LISTENURI"] = forwarderApiListenUri,
                   ["SEQCLI_FORWARDER_DIAGNOSTICS_EXPOSEINGESTIONLOG"] = "True"
               }))
        {
            await WaitForForwarderConnectionAsync(forwarderApiListenUri);

            var exit = runner.Exec($"diagnostics ingestionlog -s {forwarderApiListenUri}");
            Assert.Equal(0, exit);

            Assert.StartsWith("[20", runner.LastRunProcess!.Output);
        }
    }

    static async Task WaitForForwarderConnectionAsync(string forwarderApiListenUri)
    {
        var httpClient = new HttpClient();
        var ingestEndpoint = $"{forwarderApiListenUri}/ingest/clef";
        var content = new StringContent("", new MediaTypeHeaderValue("application/vnd.serilog.clef", "utf-8"));
        
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        
        while (true)
        {
            try
            {
                if ((await httpClient.PostAsync(ingestEndpoint, content, cts.Token)).IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // Back around the loop
            }

            await Task.Delay(100, cts.Token);
        }
    }
}

