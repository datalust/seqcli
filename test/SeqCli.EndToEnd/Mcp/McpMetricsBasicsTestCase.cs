using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModelContextProtocol.Client;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Mcp;

[CliTestCase(MinimumApiVersion = "2026.1.0")]
public class McpMetricsBasicsTestCase : McpToolTestCase
{
    [UsedImplicitly]
    record MetricDefinition(string Name, string Kind, string? Unit, string? Description);

    protected override async Task ExecuteAsync(SeqConnection connection, ILogger logger, McpClient client)
    {
        await DirectIngestion.IngestClef(connection, "'a': 1, 'b': 2, '@d': {'a': {'kind': 'Sum', 'description': 'xyz'}}");
        await DirectIngestion.IngestClef(connection, "'a': 1, 'c': 3, 'd': 4, '@d': {'a': {'kind': 'Sum','description': 'xyz'}}");
        await DirectIngestion.IngestClef(connection, "'a': 1, 'b': 5, 'e': 6, '@d': {'a': {'kind': 'Sum','description': 'xyz'}, 'e': {'kind': 'Sum', 'description': 'ghi'}}");

        var allMetrics = AssertStructuredResult<MetricDefinition[]>(await client.CallToolAsync(
            "seq_search_metric_definitions",
            new Dictionary<string, object?> { ["limit"] = 100 }));
        Assert.Equal(2, allMetrics.Length);

        var groupedByB = AssertStructuredResult<MetricDefinition[]>(await client.CallToolAsync(
            "seq_search_metric_definitions",
            new Dictionary<string, object?> { ["limit"] = 100, ["groups"] = (string[])["b"] }));
        Assert.Equal(4, groupedByB.Length);

        var filteredByDescription = AssertStructuredResult<MetricDefinition[]>(await client.CallToolAsync(
            "seq_search_metric_definitions",
            new Dictionary<string, object?>
            {
                ["limit"] = 100,
                ["predicate"] = "\"xyz\" and @Timestamp >= Now() - 1d",
                ["groups"] = (string[])["b"]
            }));
        Assert.Equal(3, filteredByDescription.Length);

        var from = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
        var to = DateTimeOffset.UtcNow.AddDays(1).ToString("o");
        var allDimensions = TextLines(AssertTextResult(await client.CallToolAsync(
            "seq_list_metric_dimensions",
            new Dictionary<string, object?> { ["limit"] = 100, ["from"] = from, ["to"] = to })));
        Assert.All(["b", "c", "d"], name => Assert.Contains(name, allDimensions));

        var dimensionsForE = AssertTextResult(await client.CallToolAsync(
            "seq_list_metric_dimensions",
            new Dictionary<string, object?> { ["limit"] = 100, ["from"] = from, ["to"] = to, ["metric"] = "e" }));
        Assert.Equal("b", dimensionsForE.Trim());

        var bValues = TextLines(AssertTextResult(await client.CallToolAsync(
            "seq_list_metric_dimension_values",
            new Dictionary<string, object?> { ["limit"] = 100, ["from"] = from, ["to"] = to, ["dimension"] = "b" })));
        Assert.All(["2", "5"], value => Assert.Contains(value, bValues));

        var unbounded = await client.CallToolAsync(
            "seq_search_metric_definitions",
            new Dictionary<string, object?> { ["limit"] = 100, ["predicate"] = "\"xyz\"" });
        Assert.True(unbounded.IsError ?? false);
    }

    static string[] TextLines(string text)
    {
        return text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
