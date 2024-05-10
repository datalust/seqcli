// Copyright © Datalust Pty Ltd and Contributors
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

using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace SeqCli.Templates.Parser;

static class JsonTemplateTokenizer
{
    static TextParser<Unit> JsonStringToken { get; } =
        from open in Character.EqualTo('"')
        from content in Character.EqualTo('\\').IgnoreThen(Character.AnyChar).Value(Unit.Value).Try()
            .Or(Character.Except('"').Value(Unit.Value))
            .IgnoreMany()
        from close in Character.EqualTo('"')
        select Unit.Value;

    static TextParser<Unit> JsonNumberToken { get; } =
        from sign in Character.EqualTo('-').OptionalOrDefault()
        from first in Character.Digit
        from rest in Character.Digit.Or(Character.In('.', 'e', 'E', '+', '-')).IgnoreMany()
        select Unit.Value;

    public static Tokenizer<JsonTemplateToken> Instance { get; } = 
        new TokenizerBuilder<JsonTemplateToken>()
            .Ignore(Span.WhiteSpace)
            .Match(Character.EqualTo('{'), JsonTemplateToken.LBracket)
            .Match(Character.EqualTo('}'), JsonTemplateToken.RBracket)
            .Match(Character.EqualTo(':'), JsonTemplateToken.Colon)
            .Match(Character.EqualTo(','), JsonTemplateToken.Comma)
            .Match(Character.EqualTo('['), JsonTemplateToken.LSquareBracket)
            .Match(Character.EqualTo(']'), JsonTemplateToken.RSquareBracket)
            .Match(Character.EqualTo('('), JsonTemplateToken.LParen)
            .Match(Character.EqualTo(')'), JsonTemplateToken.RParen)
            .Match(JsonStringToken,        JsonTemplateToken.String)
            .Match(JsonNumberToken,        JsonTemplateToken.Number, requireDelimiters: true)
            .Match(Identifier.CStyle,      JsonTemplateToken.Identifier, requireDelimiters: true)
            .Build();
}