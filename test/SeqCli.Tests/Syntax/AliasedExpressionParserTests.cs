using System;
using SeqCli.Syntax;
using Xunit;

#nullable enable

namespace SeqCli.Tests.Syntax;

public class AliasedExpressionParserTests
{
    [Theory]
    [InlineData("count(*)", "count(*)", null)]
    [InlineData("count(*) as errors", "count(*)", "errors")]
    [InlineData("phist(bucket.midpoint, bucket.count, 95) as p95", "phist(bucket.midpoint, bucket.count, 95)", "p95")]
    [InlineData("max(a as b)", "max(a as b)", null)]
    [InlineData("ServiceName ci as service", "ServiceName ci", "service")]
    [InlineData("ServiceName ci", "ServiceName ci", null)]
    public void ParsesExpressionAndAlias(string fragment, string expression, string? alias)
    {
        var parsed = AliasedExpressionParser.ParseExpression(fragment);

        Assert.Equal(expression, parsed.Expression);
        Assert.Equal(alias, parsed.Alias);
        Assert.False(parsed.IsCaseInsensitive);
    }

    [Theory]
    [InlineData("ServiceName", "ServiceName", null, false)]
    [InlineData("ServiceName ci", "ServiceName", null, true)]
    [InlineData("ServiceName as service", "ServiceName", "service", false)]
    [InlineData("ServiceName ci as service", "ServiceName", "service", true)]
    [InlineData("StartsWith(@Message, 'a') ci as sw", "StartsWith(@Message, 'a')", "sw", true)]
    public void ParsesCaseInsensitiveModifierWhenAllowed(string fragment, string expression, string? alias, bool isCaseInsensitive)
    {
        var parsed = AliasedExpressionParser.ParseExpression(fragment, allowCaseInsensitive: true);

        Assert.Equal(expression, parsed.Expression);
        Assert.Equal(alias, parsed.Alias);
        Assert.Equal(isCaseInsensitive, parsed.IsCaseInsensitive);
    }
}
