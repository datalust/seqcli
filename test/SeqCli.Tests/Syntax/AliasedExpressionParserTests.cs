using System;
using SeqCli.Syntax;
using Superpower;
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
    // The expression may span several whitespace-separated tokens, and the original internal
    // spacing is preserved.
    [InlineData("phist(bucket.midpoint, bucket.count, 95) as p95", "phist(bucket.midpoint, bucket.count, 95)", "p95")]
    // The `as` keyword is matched case-insensitively.
    [InlineData("x AS y", "x", "y")]
    // Without opting in, `ci` is not a modifier and stays part of the expression.
    [InlineData("ServiceName ci as service", "ServiceName ci", "service")]
    [InlineData("ServiceName ci", "ServiceName ci", null)]
    public void ParsesExpressionAndAlias(string fragment, string expression, string? alias)
    {
        var parsed = AliasedExpressionParser.Parse(fragment);

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
        var parsed = AliasedExpressionParser.Parse(fragment, allowCaseInsensitive: true);

        Assert.Equal(expression, parsed.Expression);
        Assert.Equal(alias, parsed.Alias);
        Assert.Equal(isCaseInsensitive, parsed.IsCaseInsensitive);
    }

    [Theory]
    // A `ci` following the alias is not the modifier the grammar permits, so it's rejected rather
    // than silently folded into the alias.
    [InlineData("ServiceName as service ci")]
    // An expression cannot contain a bare `as`, since the alias binds to a single trailing token.
    [InlineData("a as b as c")]
    // An alias must follow `as`.
    [InlineData("ServiceName as")]
    // A grouping must have an expression before the `ci` modifier.
    [InlineData("ci")]
    // There must be an expression.
    [InlineData("as service")]
    [InlineData("   ")]
    public void RejectsMalformedFragments(string fragment)
    {
        Assert.Throws<ParseException>(() => AliasedExpressionParser.Parse(fragment, allowCaseInsensitive: true));
    }

    [Fact]
    public void ErrorMessageIdentifiesTheProblem()
    {
        var ex = Assert.Throws<ParseException>(() => AliasedExpressionParser.Parse("ServiceName as"));

        // Superpower positions the error and names the missing production.
        Assert.Contains("alias", ex.Message);
    }

    [Fact]
    public void ParseRejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AliasedExpressionParser.Parse(null!));
    }
}
