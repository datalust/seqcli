using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using Seq.Api;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Mcp;

// ReSharper disable once UnusedType.Global
public class McpSignalUsageTestCase : McpToolTestCase
{
    record SignalSummary(string Id, string Title);

    // Default signals included in every Seq installation.
    static readonly SignalSummary Errors = new("signal-m33301", "Errors");
    static readonly SignalSummary Warnings = new("signal-m33302", "Warnings");
    static readonly SignalSummary Spans = new("signal-m20231011", "Spans");
    static readonly SignalSummary Logs = new("signal-m20231211", "Logs");

    protected override async Task ExecuteAsync(SeqConnection connection, ILogger logger, McpClient client)
    {
        var runId = "mcp-" + Guid.NewGuid().ToString("n");

        logger.Information("Item {ItemNumber} processed in run {RunId}", 1, runId);
        logger.Information("Item {ItemNumber} processed in run {RunId}", 2, runId);
        logger.Warning("Item {ItemNumber} delayed in run {RunId}", 3, runId);
        logger.Warning("Item {ItemNumber} delayed in run {RunId}", 4, runId);
        logger.Error("Item {ItemNumber} failed in run {RunId}", 5, runId);

        var signals = AssertStructuredResult<SignalSummary[]>(await client.CallToolAsync("seq_list_signals"));
        foreach (var signal in new[] { Errors, Warnings, Spans, Logs })
        {
            Assert.Contains(signal, signals);
        }

        var predicate = $"RunId = '{runId}' and @Timestamp >= Now() - 1d";

        // Union: the two warnings plus the error.
        Assert.Equal(3, await CountSearchResultsAsync(client, predicate, $"{Errors.Id}~{Warnings.Id}"));

        // Intersection: all of the warnings are log events, not spans.
        Assert.Equal(2, await CountSearchResultsAsync(client, predicate, $"{Warnings.Id},{Logs.Id}"));

        var query = $"select count(*) as total from stream where {predicate}";

        // Union: no spans were written, so only the error is counted.
        Assert.Equal(1, await CountQueryResultAsync(client, query, $"{Spans.Id}~{Errors.Id}"));

        // Intersection with a grouped union: warnings and errors, all of which are log events.
        Assert.Equal(3, await CountQueryResultAsync(client, query, $"({Errors.Id}~{Warnings.Id}),{Logs.Id}"));
    }

    static async Task<int> CountSearchResultsAsync(McpClient client, string predicate, string signal)
    {
        var searchResult = AssertTextResult(await client.CallToolAsync(
            "seq_search",
            new Dictionary<string, object> { ["limit"] = 10, ["predicate"] = predicate, ["signal"] = signal }));
        return OrderedSearchResultIds(searchResult).Length;
    }

    static async Task<int> CountQueryResultAsync(McpClient client, string query, string signal)
    {
        var queryResult = AssertTextResult(await client.CallToolAsync(
            "seq_query",
            new Dictionary<string, object> { ["query"] = query, ["signal"] = signal }));
        var lines = queryResult.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(2, lines.Length);
        Assert.Equal("total", lines[0]);
        return int.Parse(lines[1], CultureInfo.InvariantCulture);
    }
}
