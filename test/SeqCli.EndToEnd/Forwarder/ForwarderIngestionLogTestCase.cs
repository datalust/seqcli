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

public class ForwarderIngestionLogTestCase: ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var (proc1, listenUri1) = await runner.SpawnForwarderAsync();
        using (proc1)
        {
            var exit = runner.Exec($"diagnostics ingestionlog -s {listenUri1}");
            Assert.NotEqual(0, exit);
        }

        var (proc2, listenUri2) = await runner.SpawnForwarderAsync(environment: new()
        {
            ["SEQCLI_FORWARDER_DIAGNOSTICS_EXPOSEINGESTIONLOG"] = "True"
        });
        using (proc2)
        {
            var exit = runner.Exec($"diagnostics ingestionlog -s {listenUri2}");
            Assert.Equal(0, exit);

            Assert.StartsWith("[20", runner.LastRunProcess!.Output);
        }
    }
}
