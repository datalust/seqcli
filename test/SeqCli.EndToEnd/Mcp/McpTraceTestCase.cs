using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Mcp;

// ReSharper disable once UnusedType.Global
public class McpTraceTestCase : McpToolTestCase
{
    const string TraceId = "7d4dedcc73b18e449e0e4ea08cbe346d";

    protected override async Task ExecuteAsync(SeqConnection connection, ILogger logger, McpClient client)
    {
        var inputFile = Path.Combine("Data", "trace-tree.clef");
        Assert.True(File.Exists(inputFile));
        await DirectIngestion.IngestClefDocument(connection, await File.ReadAllTextAsync(inputFile));

        // The uppercase trace id checks that ids are normalized before querying.
        var loaded = await client.CallToolAsync(
            "seq_load_trace",
            new Dictionary<string, object>
            {
                ["trace_id"] = TraceId.ToUpperInvariant(),
                ["columns"] = new[] { "Customer" }
            });

        var text = AssertTextResult(loaded);
        Assert.Contains("Loaded 4 span(s)", text);

        var document = AssertStructuredObjectResult(loaded);
        Assert.Equal(TraceId, document.GetProperty("traceId").GetString());
        Assert.True(document.GetProperty("complete").GetBoolean());

        // The trace's only orphan is a log event, and log events are never retrieved.
        Assert.False(document.TryGetProperty("orphans", out _));

        var root = document.GetProperty("root");
        Assert.Equal("1111111111111111", root.GetProperty("spanId").GetString());
        Assert.Equal("GET /orders", root.GetProperty("message").GetString());
        Assert.Equal(1000.0, root.GetProperty("elapsedMs").GetDouble());
        Assert.Equal("scott", root.GetProperty("columns").GetProperty("Customer").GetString());

        var children = root.GetProperty("children").EnumerateArray().ToArray();
        Assert.Equal(2, children.Length);
        Assert.Equal("Query orders", children[0].GetProperty("message").GetString());
        Assert.Equal("Render response", children[1].GetProperty("message").GetString());

        // All-null column values are omitted entirely.
        Assert.False(children[0].TryGetProperty("columns", out _));

        var select = Assert.Single(children[0].GetProperty("children").EnumerateArray());
        Assert.Equal("SELECT * FROM orders", select.GetProperty("message").GetString());
        Assert.StartsWith("System.InvalidOperationException", select.GetProperty("exception").GetString());

        AssertSpansOnly(root);

        var subtree = AssertStructuredObjectResult(await client.CallToolAsync(
            "seq_load_trace",
            new Dictionary<string, object>
            {
                ["trace_id"] = TraceId,
                ["span_id"] = "2222222222222222"
            }));

        Assert.False(subtree.TryGetProperty("orphans", out _));
        var subtreeRoot = subtree.GetProperty("root");
        Assert.Equal("2222222222222222", subtreeRoot.GetProperty("spanId").GetString());
        Assert.Equal("1111111111111111", subtreeRoot.GetProperty("parentSpanId").GetString());
        var subtreeJson = subtree.GetRawText();
        Assert.DoesNotContain("GET /orders", subtreeJson);
        Assert.DoesNotContain("Render response", subtreeJson);
        Assert.Contains("System.InvalidOperationException", subtreeJson);

        await AssertErrorResultAsync(client, new() { ["trace_id"] = "not-a-trace-id" },
            "32 hexadecimal digits");
        await AssertErrorResultAsync(client, new() { ["trace_id"] = new string('0', 32) },
            "No events found");
        await AssertErrorResultAsync(client, new() { ["trace_id"] = TraceId, ["span_id"] = "not-a-span-id" },
            "16 hexadecimal digits");
        await AssertErrorResultAsync(client, new() { ["trace_id"] = TraceId, ["span_id"] = "aaaaaaaaaaaaaaaa" },
            "does not appear");
    }

    static void AssertSpansOnly(JsonElement node)
    {
        // With logs excluded there is no span/log distinction, so nodes carry no `type` discriminator.
        Assert.False(node.TryGetProperty("type", out _));
        Assert.True(node.TryGetProperty("start", out _));
        var message = node.GetProperty("message").GetString();
        Assert.NotEqual("42 rows retrieved", message);
        Assert.NotEqual("Orphan log", message);
        if (node.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                AssertSpansOnly(child);
        }
    }

    static async Task AssertErrorResultAsync(McpClient client, Dictionary<string, object> arguments, string expectedInMessage)
    {
        var result = await client.CallToolAsync("seq_load_trace", arguments);
        Assert.True(result.IsError ?? false);
        var text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        Assert.Contains(expectedInMessage, text);
    }
}
