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
using System.Text.RegularExpressions;

namespace SeqCli.Syntax;

/// <summary>
/// Splits the `&lt;expression&gt; [ci] [as &lt;alias&gt;]` fragment syntax that Seq's query language
/// accepts in `select` columns, `group by` groupings, and lateral joins into an
/// <see cref="AliasedExpression"/>.
/// </summary>
/// <remarks>
/// This is a deliberately shallow split rather than a parse of Seq's grammar: we only peel a
/// trailing `as &lt;alias&gt;` (and optional preceding `ci` modifier) off the end, leaving everything
/// before it as opaque expression text. Requiring the alias to be a bare identifier sitting at the
/// very end keeps an `as` appearing inside the expression — as in <c>max(a as b)</c> — from being
/// mistaken for an alias. Seq itself validates the resulting expression, so we don't attempt to
/// reject every malformed fragment here.
/// </remarks>
static class AliasedExpressionParser
{
    // A trailing `as <alias>`, where the alias is a bare identifier anchored to the end of the
    // fragment (so an `as` buried inside a function call isn't treated as an alias).
    static readonly Regex AliasPattern =
        new(@"\s+as\s+(?<alias>[\p{L}_][\p{L}\p{N}_]*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // A trailing `ci` case-insensitivity modifier.
    static readonly Regex CiPattern =
        new(@"\s+ci$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Parse a fragment of the form <c>&lt;expression&gt; [as &lt;alias&gt;]</c>, as accepted by query
    /// `select` columns and lateral joins. When <paramref name="allowCaseInsensitive"/> is
    /// <c>true</c>, the grammar is extended to <c>&lt;expression&gt; [ci] [as &lt;alias&gt;]</c> as used
    /// by `group by`, where the `ci` modifier appears between the expression and the optional alias.
    /// </summary>
    /// <exception cref="FormatException">The fragment has no expression before its alias.</exception>
    public static AliasedExpression ParseExpression(string fragment, bool allowCaseInsensitive = false)
    {
        if (fragment == null) throw new ArgumentNullException(nameof(fragment));

        var expression = fragment.Trim();

        string? alias = null;
        var aliasMatch = AliasPattern.Match(expression);
        if (aliasMatch.Success)
        {
            alias = aliasMatch.Groups["alias"].Value;
            expression = expression[..aliasMatch.Index].TrimEnd();
        }

        var isCaseInsensitive = false;
        if (allowCaseInsensitive)
        {
            var ciMatch = CiPattern.Match(expression);
            if (ciMatch.Success)
            {
                isCaseInsensitive = true;
                expression = expression[..ciMatch.Index].TrimEnd();
            }
        }

        if (expression.Length == 0)
            throw new FormatException($"The fragment `{fragment}` has no expression.");

        return new AliasedExpression(expression, alias, isCaseInsensitive);
    }
}
