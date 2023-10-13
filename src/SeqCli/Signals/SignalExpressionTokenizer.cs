// Copyright 2018 Datalust Pty Ltd
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

using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.Signals;

class SignalExpressionTokenizer : Tokenizer<SignalExpressionToken>
{
    static readonly Dictionary<char, SignalExpressionToken> Operators = new()
    {
        [','] = SignalExpressionToken.Comma,
        ['~'] = SignalExpressionToken.Tilde,
        ['('] = SignalExpressionToken.LParen,
        [')'] = SignalExpressionToken.RParen
    };

    static readonly TextParser<TextSpan> Id = Span.WithoutAny(ch => Operators.ContainsKey(ch) || char.IsWhiteSpace(ch));

    protected override IEnumerable<Result<SignalExpressionToken>> Tokenize(TextSpan span)
    {
        var next = SkipWhiteSpace(span);
        if (!next.HasValue)
        {
            yield break;
        }

        do
        {
            if (Operators.TryGetValue(next.Value, out var token))
            {
                yield return Result.Value(token, next.Location, next.Remainder);
                next = next.Remainder.ConsumeChar();
            }
            else
            {
                var id = Id(next.Location);
                next = id.Remainder.ConsumeChar();

                yield return Result.Value(SignalExpressionToken.Id, id.Location, id.Remainder);
            }

            next = SkipWhiteSpace(next.Location);
        } while (next.HasValue);
    }
}
