using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Events;

public class SearchColumnsTestCase : ICliTestCase
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

        var filter = $"--filter=\"@TraceId = '{TraceId}' and Customer is not null\"";

        exit = runner.Exec("search", $"{filter} -c 10 --column Customer --column RowCount");
        Assert.Equal(0, exit);
        Assert.Contains("] scott GET /orders", runner.LastRunProcess!.Output);

        // Columns apply to plain-text output only.
        exit = runner.Exec("search", $"{filter} -c 10 --column Customer --json");
        Assert.Equal(0, exit);
        Assert.Contains("GET {Route}", runner.LastRunProcess!.Output);
        Assert.DoesNotContain("_SeqcliColumn", runner.LastRunProcess!.Output);

        exit = runner.Exec("search", $"{filter} -c 10 --column \"not a valid (\"");
        Assert.Equal(1, exit);
        Assert.Contains("could not be compiled", runner.LastRunProcess!.Output);

        return Task.CompletedTask;
    }
}
