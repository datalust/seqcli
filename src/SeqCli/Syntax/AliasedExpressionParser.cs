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

static partial class AliasedExpressionParser
{
    // " as <alias>"
    [GeneratedRegex(@"\s+as\s+(?<alias>[\p{L}_][\p{L}\p{N}_]*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AsAlias();
    
    // " ci"
    [GeneratedRegex(@"\s+ci$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Ci();

    public static AliasedExpression ParseExpression(string fragment, bool allowCaseInsensitive = false)
    {
        if (fragment == null) throw new ArgumentNullException(nameof(fragment));

        var expression = fragment.Trim();
        
        string? alias = null;
        var aliasMatch = AsAlias().Match(expression);
        if (aliasMatch.Success)
        {
            alias = aliasMatch.Groups["alias"].Value;
            expression = expression[..aliasMatch.Index].TrimEnd();
        }

        var isCaseInsensitive = false;
        if (allowCaseInsensitive)
        {
            var ciMatch = Ci().Match(expression);
            if (ciMatch.Success)
            {
                isCaseInsensitive = true;
                expression = expression[..ciMatch.Index].TrimEnd();
            }
        }

        return expression.Length == 0 ? throw new FormatException($"The fragment `{fragment}` has no expression.") : new AliasedExpression(expression, alias, isCaseInsensitive);
    }
}
