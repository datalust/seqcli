// Copyright © Datalust and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.Syntax;

/// <summary>
/// Parses the `&lt;expression&gt; [ci] [as &lt;alias&gt;]` fragment syntax that Seq's query language
/// accepts in `select` columns, `group by` groupings, and lateral joins into an
/// <see cref="AliasedExpression"/>. Following the pattern used by
/// <see cref="Signals.SignalExpressionParser"/>, it tokenizes with Superpower so that malformed
/// input produces a positioned error message rather than being silently misinterpreted.
/// </summary>
static class AliasedExpressionParser
{
    static readonly AliasedExpressionTokenizer Tokenizer = new();

    // `as <alias>`
    static readonly TokenListParser<AliasedExpressionToken, string?> Alias =
        from _ in Token.EqualTo(AliasedExpressionToken.As)
        from alias in Token.EqualTo(AliasedExpressionToken.Identifier).Named("an alias")
        select (string?)alias.ToStringValue();

    // The grammar used by `group by`: `<expression> [ci] [as <alias>]`, where the `ci` modifier
    // sits between the expression and the optional alias.
    static readonly TokenListParser<AliasedExpressionToken, AliasedExpression> CaseInsensitiveAware =
        (from expression in Token.EqualTo(AliasedExpressionToken.Identifier).AtLeastOnce().Named("an expression")
            from isCaseInsensitive in Token.EqualTo(AliasedExpressionToken.Ci).Value(true).OptionalOrDefault(false)
            from alias in Alias.OptionalOrDefault()
            select new AliasedExpression(SpanText(expression), alias, isCaseInsensitive))
        .AtEnd();

    // The grammar used by `select` columns and lateral joins: `<expression> [as <alias>]`. Here
    // `ci` is not a modifier, so a bare `ci` is accepted as ordinary expression text.
    static readonly TokenListParser<AliasedExpressionToken, AliasedExpression> CaseSensitiveOnly =
        (from expression in Token.EqualTo(AliasedExpressionToken.Identifier)
                .Or(Token.EqualTo(AliasedExpressionToken.Ci)).AtLeastOnce().Named("an expression")
            from alias in Alias.OptionalOrDefault()
            select new AliasedExpression(SpanText(expression), alias, false))
        .AtEnd();

    /// <summary>
    /// Parse a fragment of the form <c>&lt;expression&gt; [as &lt;alias&gt;]</c>, as accepted by query
    /// `select` columns and lateral joins. When <paramref name="allowCaseInsensitive"/> is
    /// <c>true</c>, the grammar is extended to <c>&lt;expression&gt; [ci] [as &lt;alias&gt;]</c> as used
    /// by `group by`, where the `ci` modifier appears between the expression and the optional alias.
    /// </summary>
    /// <exception cref="Superpower.ParseException">The fragment is malformed.</exception>
    public static AliasedExpression Parse(string fragment, bool allowCaseInsensitive = false)
    {
        if (fragment == null) throw new ArgumentNullException(nameof(fragment));

        var tokens = Tokenizer.Tokenize(fragment);
        var grammar = allowCaseInsensitive ? CaseInsensitiveAware : CaseSensitiveOnly;
        return grammar.Parse(tokens);
    }

    static string SpanText(Token<AliasedExpressionToken>[] expression)
    {
        // Reconstruct the original expression text, preserving internal spacing, from the first
        // through last of its tokens.
        var first = expression[0].Span;
        var last = expression[^1].Span;
        return first.Source!.Substring(first.Position.Absolute, last.Position.Absolute + last.Length - first.Position.Absolute);
    }
}
