using SeqCli.Mcp.Formatting;
using Xunit;

namespace SeqCli.Tests.Mcp;

public class SeqSyntaxFormatterTests
{
    [Theory]
    [InlineData("@Properties", "a", true, "a")]
    [InlineData("@Properties", "a b", true, "@Properties['a b']")]
    [InlineData("@Properties", "and", true, "@Properties.and")]
    [InlineData("@Resource", "a", false, "@Resource.a")]
    [InlineData("@Resource", "a b", false, "@Resource['a b']")]
    [InlineData("@Resource", "and", false, "@Resource.and")]
    public void IdentifiersAreIdiomaticallyFormatted(string prefix, string name, bool prefixIsOptional, string expected)
    {
        var actual = SeqSyntaxFormatter.MakeIdentifier(prefix, name, prefixIsOptional);
        Assert.Equal(expected, actual);
    }
}