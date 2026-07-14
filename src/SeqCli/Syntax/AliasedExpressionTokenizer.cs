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
using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.Syntax;

class AliasedExpressionTokenizer : Tokenizer<AliasedExpressionToken>
{
    // A token is a maximal run of non-whitespace characters. The `as` and `ci` keywords are
    // recognised as whitespace-delimited words; everything else is opaque expression text (an
    // expression may span several such tokens when it contains internal whitespace).
    static readonly TextParser<TextSpan> Word = Span.WithoutAny(char.IsWhiteSpace);

    protected override IEnumerable<Result<AliasedExpressionToken>> Tokenize(TextSpan span)
    {
        var next = SkipWhiteSpace(span);
        if (!next.HasValue)
            yield break;

        do
        {
            var word = Word(next.Location);

            var text = word.Value.ToStringValue();
            var kind = text.Equals("as", StringComparison.OrdinalIgnoreCase) ? AliasedExpressionToken.As
                : text.Equals("ci", StringComparison.OrdinalIgnoreCase) ? AliasedExpressionToken.Ci
                : AliasedExpressionToken.Identifier;

            yield return Result.Value(kind, word.Location, word.Remainder);

            next = SkipWhiteSpace(word.Remainder);
        } while (next.HasValue);
    }
}
