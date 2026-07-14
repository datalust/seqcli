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

namespace SeqCli.Syntax;

/// <summary>
/// An expression paired with the optional alias and case-insensitivity modifier that Seq's query
/// language accepts in `select` columns, `group by` groupings, and lateral joins. Parse fragments
/// into this shape with <see cref="AliasedExpressionParser"/>.
/// </summary>
readonly struct AliasedExpression
{
    public AliasedExpression(string expression, string? alias, bool isCaseInsensitive)
    {
        Expression = expression;
        Alias = alias;
        IsCaseInsensitive = isCaseInsensitive;
    }

    /// <summary>
    /// The expression appearing before any `as` alias.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// The alias given after `as`, or <c>null</c> when none was specified.
    /// </summary>
    public string? Alias { get; }

    /// <summary>
    /// <c>true</c> when the `ci` case-insensitivity modifier was present.
    /// </summary>
    public bool IsCaseInsensitive { get; }
}
