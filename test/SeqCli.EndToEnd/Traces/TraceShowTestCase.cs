using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Traces;

public class TraceShowTestCase : ICliTestCase
{
    const string TraceId = "7d4dedcc73b18e449e0e4ea08cbe346d";

    public Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = Path.Combine("Data", "trace-tree.clef");
        Assert.True(File.Exists(inputFile));

        var exit = runner.Exec("ingest", $"--json -i {inputFile}");
        Assert.Equal(0, exit);

        exit = runner.Exec("trace show", $"-i {TraceId} --logs --column Customer");
        Assert.Equal(0, exit);

        var output = runner.LastRunProcess!.Output;
        var expected = new[]
        {
            "] scott GET /orders (1000 ms)",
            "] ├─ Query orders (300 ms)",
            "] │  ├─ SELECT * FROM orders (150 ms)",
            "] │  ┊  42 rows retrieved",
            "] └─ Render response (400 ms)",
            "] ┊  Orphan log"
        };

        var lastIndex = -1;
        foreach (var line in expected)
        {
            var index = output.IndexOf(line, StringComparison.Ordinal);
            Assert.True(index > lastIndex, $"Expected `{line}` in order in output: {output}");
            lastIndex = index;
        }

        Assert.DoesNotContain("System.TimeoutException", output);

        exit = runner.Exec("trace show", $"-i {TraceId} --logs --exceptions");
        Assert.Equal(0, exit);
        Assert.Contains("System.TimeoutException: The query timeout was reached", runner.LastRunProcess!.Output);

        exit = runner.Exec("trace show", $"-i {TraceId}");
        Assert.Equal(0, exit);
        Assert.DoesNotContain("42 rows retrieved", runner.LastRunProcess!.Output);

        exit = runner.Exec("trace show", "-i not-a-trace-id");
        Assert.Equal(1, exit);

        exit = runner.Exec("trace show", "-i 00000000000000000000000000000000");
        Assert.Equal(1, exit);

        return Task.CompletedTask;
    }
}
