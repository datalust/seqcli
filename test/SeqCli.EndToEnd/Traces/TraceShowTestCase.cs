using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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

        exit = runner.Exec("trace", $"-i {TraceId} --logs --column Customer");
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

        exit = runner.Exec("trace", $"-i {TraceId} --logs --exceptions");
        Assert.Equal(0, exit);
        Assert.Contains("System.TimeoutException: The query timeout was reached", runner.LastRunProcess!.Output);

        exit = runner.Exec("trace", $"-i {TraceId}");
        Assert.Equal(0, exit);
        Assert.DoesNotContain("42 rows retrieved", runner.LastRunProcess!.Output);

        exit = runner.Exec("trace", "-i not-a-trace-id");
        Assert.Equal(1, exit);

        exit = runner.Exec("trace", "-i 00000000000000000000000000000000");
        Assert.Equal(1, exit);

        exit = runner.Exec("trace", $"-i {TraceId} --span-id 2222222222222222 --logs");
        Assert.Equal(0, exit);
        output = runner.LastRunProcess!.Output;
        Assert.Contains("] Query orders (300 ms)", output);
        Assert.Contains("SELECT * FROM orders", output);
        Assert.DoesNotContain("GET /orders", output);
        Assert.DoesNotContain("Render response", output);

        exit = runner.Exec("trace", $"-i {TraceId} --span-id not-a-span-id");
        Assert.Equal(1, exit);

        exit = runner.Exec("trace", $"-i {TraceId} --span-id aaaaaaaaaaaaaaaa");
        Assert.Equal(1, exit);
        Assert.Contains("does not appear", runner.LastRunProcess!.Output);

        exit = runner.Exec("trace", $"-i {TraceId} --logs --json --column Customer");
        Assert.Equal(0, exit);
        var document = ParseDocument(runner.LastRunProcess!.Output);

        Assert.Equal(TraceId, (string?)document["traceId"]);
        Assert.True((bool?)document["complete"]);

        var root = (JObject)document["root"]!;
        Assert.Equal("span", (string?)root["type"]);
        Assert.Equal("1111111111111111", (string?)root["spanId"]);
        Assert.Equal("GET /orders", (string?)root["message"]);
        Assert.Equal(1000.0, (double?)root["elapsedMs"]);
        Assert.Equal("scott", (string?)root["columns"]!["Customer"]);

        var query = (JObject)((JArray)root["children"]!).Single(c => (string?)c["message"] == "Query orders");
        Assert.Equal("1111111111111111", (string?)query["parentSpanId"]);
        Assert.Equal(
            ["SELECT * FROM orders", "42 rows retrieved"],
            ((JArray)query["children"]!).Select(c => (string)c["message"]!).ToArray());

        var orphan = (JObject)Assert.Single((JArray)document["orphans"]!);
        Assert.Equal("log", (string?)orphan["type"]);
        Assert.Equal("Orphan log", (string?)orphan["message"]);
        Assert.Equal("9999999999999999", (string?)orphan["spanId"]);

        exit = runner.Exec("trace", $"-i {TraceId} --span-id 2222222222222222 --logs --json");
        Assert.Equal(0, exit);
        document = ParseDocument(runner.LastRunProcess!.Output);

        Assert.Null(document["orphans"]);
        Assert.Equal("2222222222222222", (string?)document["root"]!["spanId"]);
        Assert.Equal("1111111111111111", (string?)document["root"]!["parentSpanId"]);

        return Task.CompletedTask;
    }

    // The captured output interleaves any logged warnings with the JSON document; the document is
    // the only braced content.
    static JObject ParseDocument(string output)
    {
        var start = output.IndexOf('{');
        var end = output.LastIndexOf('}');
        Assert.True(start >= 0 && end > start, $"Expected a JSON document in output: {output}");
        return JObject.Parse(output.Substring(start, end - start + 1));
    }
}
