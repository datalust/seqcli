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

using Seq.Api.Model.Signals;
using Superpower;
using Superpower.Parsers;

namespace SeqCli.Signals;

static class SignalExpressionParser
{
    // intersect: `,`
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionKind> Intersect =
        Token.EqualTo(SignalExpressionToken.Comma).Value(SignalExpressionKind.Intersection);

    // union: `~`
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionKind> Union =
        Token.EqualTo(SignalExpressionToken.Tilde).Value(SignalExpressionKind.Union);

    // operator: `intersect` | `union`
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionKind> Operator = Intersect.Or(Union);

    // concrete: `signalid`
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionPart> SignalId =
        from id in Token.EqualTo(SignalExpressionToken.Id)
        select SignalExpressionPart.Signal(id.ToStringValue());

    // brackets: `(` => `operation` => `)`
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionPart> Brackets =
        from lparen in Token.EqualTo(SignalExpressionToken.LParen)
        from expr in Parse.Ref(() => Operation!)
        from rparen in Token.EqualTo(SignalExpressionToken.RParen)
        select expr;

    // subexpr: `brackets` | `id`
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionPart> SubExpression =
        Brackets.Or(SignalId).Named("expression");

    // operation: ( `subexpr` => `operator` => `subexpr` )*
    static readonly TokenListParser<SignalExpressionToken, SignalExpressionPart> Operation =
        Parse.Chain(Operator, SubExpression, (kind, lhs, rhs) => new SignalExpressionPart
        {
            Kind = kind,
            Left = lhs,
            Right = rhs
        });

    static readonly TokenListParser<SignalExpressionToken, SignalExpressionPart> SignalExpression = Operation.AtEnd();

    static readonly SignalExpressionTokenizer Tokenizer = new();

    public static SignalExpressionPart ParseExpression(string input)
    {
        var tokens = Tokenizer.Tokenize(input);
        return SignalExpression.Parse(tokens);
    }
}
