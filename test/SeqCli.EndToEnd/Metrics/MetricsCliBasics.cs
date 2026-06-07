using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Metrics;

[CliTestCase(MinimumApiVersion = "2026.1.0")]
class MetricsCliBasics: ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        await DirectIngestion.IngestClef(connection, "'a': 1, 'b': 2, '@d': {'a': {'kind': 'Sum', 'description': 'xyz'}}");
        await DirectIngestion.IngestClef(connection, "'a': 1, 'c': 3, 'd': 4, '@d': {'a': {'kind': 'Sum','description': 'xyz'}}");
        await DirectIngestion.IngestClef(connection, "'a': 1, 'b': 5, 'e': 6, '@d': {'a': {'kind': 'Sum','description': 'xyz'}, 'e': {'kind': 'Sum', 'description': 'ghi'}}");
        
        Assert.Equal(2, SearchResultLines(runner).Count());
        Assert.Equal(4, SearchResultLines(runner, groups: ["b"]).Count());
        Assert.Equal(3, SearchResultLines(runner, filter: "\"xyz\"", groups: ["b"]).Count());
        
        Assert.Equal(0, runner.Exec("metrics dimensions"));
        var allDimensions = runner.LastRunProcess!.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.All(["b", "c", "d"], name => Assert.Contains(allDimensions, l => l.Trim() == name));

        Assert.Equal(0, runner.Exec("metrics dimensions", "--metric e"));
        Assert.Equal("b", runner.LastRunProcess!.Output.Trim());
        
        Assert.Equal(0, runner.Exec("metrics dimension", "--accessor b"));
        var bValues = runner.LastRunProcess!.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        Assert.All(["2", "5"], value => Assert.Contains(bValues, l => l.Trim() == value));
    }

    static IEnumerable<string> SearchResultLines(CliCommandRunner runner, string? filter = null, string[]? groups = null)
    {
        var args = "";
        if (filter != null)
            args += $"--filter=\"{filter.Replace("\"", "\\\"")}\"";
        foreach (var group in groups ?? [])
            args += $" --group=\"{group.Replace("\"", "\\\"")}\"";
        
        Assert.Equal(0, runner.Exec("metrics search", args));
        var reader = new StringReader(runner.LastRunProcess!.Output);
        var skippedHeading = false;
        while (reader.ReadLine() is { } line)
        {
            if (!skippedHeading)
            {
                skippedHeading = true;
            }
            else
            {
                yield return line!;
            }
        }
    }
}