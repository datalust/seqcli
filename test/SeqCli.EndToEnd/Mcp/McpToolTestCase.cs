using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Mcp;

/// <summary>
/// Base class for test cases exercising the tools provided by <c>seqcli mcp run</c>. The MCP server
/// is spawned over stdio and supplied to the subclass as a connected <see cref="McpClient"/>.
/// </summary>
public abstract partial class McpToolTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "seqcli mcp run",
            Command = "dotnet",
            Arguments = [TestConfiguration.TestedBinary, "mcp", "run", $"--server={connection.Client.ServerUrl}"]
        });

        await using var client = await McpClient.CreateAsync(transport);

        await ExecuteAsync(connection, logger, client);
    }

    protected abstract Task ExecuteAsync(SeqConnection connection, ILogger logger, McpClient client);

    protected static string AssertTextResult(CallToolResult callToolResult)
    {
        var text = string.Join("\n", callToolResult.Content.OfType<TextContentBlock>().Select(c => c.Text));
        Assert.False(callToolResult.IsError ?? false, text);
        return text;
    }

    protected static T AssertStructuredResult<T>(CallToolResult callToolResult)
    {
        AssertTextResult(callToolResult);
        Assert.NotNull(callToolResult.StructuredContent);

        // Tools returning non-object values have them wrapped in a `result` property by the MCP
        // SDK, because the protocol requires `structuredContent` to be an object.
        var result = callToolResult.StructuredContent.Value.GetProperty("result");
        return JsonSerializer.Deserialize<T>(result, JsonSerializerOptions.Web)!;
    }

    protected static string[] OrderedSearchResultIds(string searchResult)
    {
        return ResultIdRegex().Matches(searchResult).Select(m => m.Value).Distinct().ToArray();
    }

    [GeneratedRegex("R[0-9A-F]+")]
    private static partial Regex ResultIdRegex();
}
