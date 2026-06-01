using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Mcp;

// ReSharper disable once UnusedType.Global
public partial class McpSessionBasicsTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var runId = "mcp-" + Guid.NewGuid().ToString("n");

        var orders = new[]
        {
            (Number: 1, Amount: 2, Customer: new { Name = "Alice", Tier = "gold", Address = new { City = "Sydney" } }),
            (Number: 2, Amount: 1, Customer: new { Name = "Bob", Tier = "silver", Address = new { City = "Hobart" } }),
            (Number: 3, Amount: 3, Customer: new { Name = "Carol", Tier = "gold", Address = new { City = "Perth" } }),
        };

        foreach (var order in orders)
        {
            logger.Information("Order {OrderNumber} in run {RunId} placed by {@Customer} for {Amount} unit(s)",
                order.Number, runId, order.Customer, order.Amount);
        }

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "seqcli mcp run",
            Command = "dotnet",
            Arguments = [TestConfiguration.TestedBinary, "mcp", "run", $"--server={connection.Client.ServerUrl}"]
        });

        await using var client = await McpClient.CreateAsync(transport);

        var predicate = $"RunId = '{runId}' and Customer.Tier = 'gold' and @Timestamp >= Now() - 1d";
        var searchResult = AssertTextResult(await client.CallToolAsync(
            "seq_search",
            new Dictionary<string, object> { ["limit"] = 10, ["predicate"] = predicate }));
        var resultIds = ResultIdRegex().Matches(searchResult).Select(m => m.Value).Distinct().ToArray();
        Assert.Equal(orders.Count(o => o.Customer.Tier == "gold"), resultIds.Length);

        var detailResult = AssertTextResult(await client.CallToolAsync(
            "seq_read_search_result",
            new Dictionary<string, object> { ["result_id"] = resultIds[0] }));
        Assert.Contains(runId, detailResult);
        Assert.Contains("Name: 'Carol'", detailResult);

        var schemaResult = AssertTextResult(await client.CallToolAsync("seq_inspect_result_schema"));
        foreach (var expectedPath in new[]
                     { "OrderNumber", "RunId", "Amount", "Customer", "Customer.Name", "Customer.Tier", "Customer.Address.City" })
            Assert.Contains(expectedPath, schemaResult);

        var query = $"select sum(Amount) as Total from stream where RunId = '{runId}' and @Timestamp >= Now() - 1d";
        var queryResult = AssertTextResult(await client.CallToolAsync(
            "seq_query",
            new Dictionary<string, object> { ["query"] = query }));
        Assert.Contains("Total", queryResult);
        Assert.Contains("6", queryResult);

        await client.CallToolAsync("seq_new_session");

        var staleResult = await client.CallToolAsync(
            "seq_read_search_result",
            new Dictionary<string, object> { ["result_id"] = resultIds[0] });
        Assert.True(staleResult.IsError ?? false);
    }

    static string AssertTextResult(CallToolResult callToolResult)
    {
        var text = string.Join("\n", callToolResult.Content.OfType<TextContentBlock>().Select(c => c.Text));
        Assert.False(callToolResult.IsError ?? false, text);
        return text;
    }

    [GeneratedRegex("R[0-9a-zA-Z]+")]
    private static partial Regex ResultIdRegex();
}
