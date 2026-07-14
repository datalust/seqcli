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
    [InlineData("  spaced   as   label  ", "spaced", "label")]
    [InlineData("ServiceName", "ServiceName", null)]
    // The expression may contain internal whitespace, which is preserved.
    [InlineData("phist(bucket.midpoint, bucket.count, 95) as p95", "phist(bucket.midpoint, bucket.count, 95)", "p95")]
    // The `as` keyword is matched case-insensitively.
    [InlineData("x AS y", "x", "y")]
    // An `as` buried inside the expression is not an alias: the alias must be a bare identifier at
    // the very end, so `max(a as b)` stays whole rather than being split into `max(a`/`b`.
    [InlineData("max(a as b)", "max(a as b)", null)]
    // Without opting in, `ci` is not a modifier and stays part of the expression.
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
    // The expression may contain the `as` keyword-like text inside a function call, and the `ci`
    // modifier is only recognised in the position the grammar allows.
    [InlineData("StartsWith(@Message, 'a') ci as sw", "StartsWith(@Message, 'a')", "sw", true)]
    // The `ci` modifier is matched case-insensitively.
    [InlineData("Name CI", "Name", null, true)]
    public void ParsesCaseInsensitiveModifierWhenAllowed(string fragment, string expression, string? alias, bool isCaseInsensitive)
    {
        var parsed = AliasedExpressionParser.ParseExpression(fragment, allowCaseInsensitive: true);

        Assert.Equal(expression, parsed.Expression);
        Assert.Equal(alias, parsed.Alias);
        Assert.Equal(isCaseInsensitive, parsed.IsCaseInsensitive);
    }

    [Theory]
    // An empty fragment has no expression; any other malformed fragment is left for Seq to reject
    // downstream.
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsFragmentsWithNoExpression(string fragment)
    {
        Assert.Throws<FormatException>(() => AliasedExpressionParser.ParseExpression(fragment, allowCaseInsensitive: true));
    }

    [Fact]
    public void ParseRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AliasedExpressionParser.ParseExpression(null!));
    }
}
