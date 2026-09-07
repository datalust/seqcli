using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Events;

public class TailColumnsTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = Path.Combine("Data", "trace-tree.clef");
        Assert.True(File.Exists(inputFile));

        var exit = runner.Exec("signal create", "-t Orders -f \"@TraceId is not null\" -c Customer -c RowCount");
        Assert.Equal(0, exit);

        var signals = await connection.Signals.ListAsync(shared: true);
        var orders = signals.Single(s => s.Title == "Orders").Id;

        var filter = "--filter=\"Customer is not null\"";

        // A column expression that can't be compiled is reported.
        exit = runner.Exec("tail", "--column \"not a valid (\"");
        Assert.Equal(1, exit);
        Assert.Contains("could not be compiled", runner.LastRunProcess!.Output);

        // Signal columns precede those specified with `--column`.
        using (var tail = runner.Spawn("tail", $"--signal {orders} {filter} --column \"@Level\""))
        {
            await IngestUntilTailWrites(runner, tail, inputFile, "] scott Information GET /orders");
        }

        // `--no-signal-columns` drops the signal's columns, but not those specified with `--column`.
        using (var tail = runner.Spawn("tail", $"--signal {orders} {filter} --no-signal-columns --column \"@Level\""))
        {
            await IngestUntilTailWrites(runner, tail, inputFile, "] Information GET /orders");
            Assert.DoesNotContain("scott", tail.Output);
        }
    }

    // Events ingested before the tail command's streaming connection is established won't be
    // observed, so ingest the test data repeatedly until the expected line appears.
    static async Task IngestUntilTailWrites(CliCommandRunner runner, CaptiveProcess tail, string inputFile, string expected)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!tail.Output.Contains(expected))
        {
            if (DateTime.UtcNow > deadline)
                Assert.Fail($"Timed out waiting for `{expected}` in: {tail.Output}");

            var exit = runner.Exec("ingest", $"--json -i {inputFile}");
            Assert.Equal(0, exit);

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}
