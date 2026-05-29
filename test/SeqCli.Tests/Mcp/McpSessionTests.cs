using SeqCli.Mcp;
using Xunit;

namespace SeqCli.Tests.Mcp;

public class McpSessionTests
{
    [Fact]
    public void ResultIdsRoundTrip()
    {
        const int id = 1245;
        var formatted = McpSession.FormatResultId(id);
        Assert.True(McpSession.TryParseResultId(formatted, out var rt));
        Assert.Equal(id, rt);
    }
}