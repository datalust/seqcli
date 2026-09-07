using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Events;

public class SearchSignalColumnsTestCase : ICliTestCase
{
    const string TraceId = "7d4dedcc73b18e449e0e4ea08cbe346d";

    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = Path.Combine("Data", "trace-tree.clef");
        Assert.True(File.Exists(inputFile));

        var exit = runner.Exec("ingest", $"--json -i {inputFile}");
        Assert.Equal(0, exit);

        exit = runner.Exec("signal create", "-t Orders -f \"@TraceId is not null\" -c Customer -c RowCount");
        Assert.Equal(0, exit);

        exit = runner.Exec("signal create", "-t Rows -f \"RowCount is not null\" -c \"RowCount * 2\"");
        Assert.Equal(0, exit);

        exit = runner.Exec("signal create", "-t Unadorned -f \"@TraceId is not null\"");
        Assert.Equal(0, exit);

        var signals = await connection.Signals.ListAsync(shared: true);
        var orders = signals.Single(s => s.Title == "Orders").Id;
        var rows = signals.Single(s => s.Title == "Rows").Id;
        var unadorned = signals.Single(s => s.Title == "Unadorned").Id;

        var filter = $"--filter=\"@TraceId = '{TraceId}'\"";

        // The signal's columns are displayed, in the order the signal declares them.
        exit = runner.Exec("search", $"--signal {orders} {filter} -c 10");
        Assert.Equal(0, exit);
        var output = runner.LastRunProcess!.Output;
        Assert.Contains("] scott GET /orders", output);
        Assert.Contains("] 42 42 rows retrieved", output);

        // Signal columns precede any specified with `--column`.
        exit = runner.Exec("search", $"--signal {orders} {filter} -c 10 --column \"@Level\"");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("] scott Information GET /orders", output);
        Assert.Contains("] 42 Warning 42 rows retrieved", output);

        // `--no-signal-columns` drops the signal's columns, but not those specified with `--column`.
        exit = runner.Exec("search", $"--signal {orders} {filter} -c 10 --no-signal-columns");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("] GET /orders", output);
        Assert.DoesNotContain("scott", output);

        exit = runner.Exec("search", $"--signal {orders} {filter} -c 10 --no-signal-columns --column \"@Level\"");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("] Information GET /orders", output);
        Assert.DoesNotContain("scott", output);

        // Signal columns apply to plain-text output only.
        exit = runner.Exec("search", $"--signal {orders} {filter} -c 10 --json");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("GET {Route}", output);
        Assert.DoesNotContain("_SeqcliColumn", output);

        // Columns are collected from every signal referenced by the expression.
        exit = runner.Exec("search", $"--signal {orders},{rows} {filter} -c 10");
        Assert.Equal(0, exit);
        Assert.Contains("] 42 84 42 rows retrieved", runner.LastRunProcess!.Output);

        exit = runner.Exec("search", $"--signal \"{orders}~{rows}\" {filter} -c 10");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("] scott GET /orders", output);
        Assert.Contains("] 42 84 42 rows retrieved", output);

        // A signal without columns contributes none.
        exit = runner.Exec("search", $"--signal {unadorned} {filter} -c 10");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("] GET /orders", output);
        Assert.DoesNotContain("scott", output);

        // A signal that can't be found is reported, rather than silently ignored.
        exit = runner.Exec("search", $"--signal signal-999999 {filter} -c 10");
        Assert.Equal(1, exit);
        Assert.Contains("Could not retrieve search result", runner.LastRunProcess!.Output);
    }
}
