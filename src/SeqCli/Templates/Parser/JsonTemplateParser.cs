// Copyright Datalust Pty Ltd and Contributors
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
using System.Diagnostics.CodeAnalysis;
using SeqCli.Templates.Ast;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.Templates.Parser;

static class JsonTemplateParser
{
    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonString { get; } =
        Token.EqualTo(JsonTemplateToken.String)
            .Apply(JsonTemplateTextParsers.String)
            .Select(s => (JsonTemplate)new JsonTemplateString(s));
        
    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonNumber { get; } =
        Token.EqualTo(JsonTemplateToken.Number)
            .Apply(JsonTemplateTextParsers.Number)
            .Select(n => (JsonTemplate)new JsonTemplateNumber(n));

    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonObject { get; } =
        from begin in Token.EqualTo(JsonTemplateToken.LBracket)
        from members in JsonString
            .Named("property name")
            .Then(name => Token.EqualTo(JsonTemplateToken.Colon)
                .IgnoreThen(Parse.Ref(() => JsonTemplateValue!)
                    .Select(value => KeyValuePair.Create(((JsonTemplateString)name).Value, value))))    
            .ManyDelimitedBy(Token.EqualTo(JsonTemplateToken.Comma),
                end: Token.EqualTo(JsonTemplateToken.RBracket))
        select (JsonTemplate)new JsonTemplateObject(new Dictionary<string, JsonTemplate>(members));

    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonArray { get; } =
        from begin in Token.EqualTo(JsonTemplateToken.LSquareBracket)
        from elements in Parse.Ref(() => JsonTemplateValue!)
            .ManyDelimitedBy(Token.EqualTo(JsonTemplateToken.Comma),
                end: Token.EqualTo(JsonTemplateToken.RSquareBracket))
        select (JsonTemplate)new JsonTemplateArray(elements);

    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonTrue { get; } =
        Token.EqualToValue(JsonTemplateToken.Identifier, "true").Value((JsonTemplate)new JsonTemplateBoolean(true));
        
    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonFalse { get; } =
        Token.EqualToValue(JsonTemplateToken.Identifier, "false").Value((JsonTemplate)new JsonTemplateBoolean(false));

    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonNull { get; } =
        Token.EqualToValue(JsonTemplateToken.Identifier, "null").Value((JsonTemplate)new JsonTemplateNull());
        
    static TokenListParser<JsonTemplateToken, JsonTemplate> Call { get; } =
        (from name in Token.EqualTo(JsonTemplateToken.Identifier)
            from lparen in Token.EqualTo(JsonTemplateToken.LParen)
            from args in Parse.Ref(() => JsonTemplateValue!).ManyDelimitedBy(Token.EqualTo(JsonTemplateToken.Comma))
            from rparen in Token.EqualTo(JsonTemplateToken.RParen)
            select (JsonTemplate)new JsonTemplateCall(name.ToStringValue(), args)).Named("function");

    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonTemplateValue { get; } =
        JsonString
            .Or(JsonNumber)
            .Or(JsonObject)
            .Or(JsonArray)
            .Or(JsonTrue)
            .Or(JsonFalse)
            .Or(JsonNull)
            .Or(Call)
            .Named("JSON value");

    static TokenListParser<JsonTemplateToken, JsonTemplate> JsonTemplate { get; } = JsonTemplateValue.AtEnd();

    public static bool TryParse(string json, [NotNullWhen(true)] out JsonTemplate? value, [NotNullWhen(false)] out string? error, out Position errorPosition)
    {
        var tokens = JsonTemplateTokenizer.Instance.TryTokenize(json);
        if (!tokens.HasValue)
        {
            value = null;
            error = tokens.ToString();
            errorPosition = tokens.ErrorPosition;
            return false;
        }

        var parsed = JsonTemplate.TryParse(tokens.Value);
        if (!parsed.HasValue)
        {
            value = null;
            error = parsed.ToString();
            errorPosition = parsed.ErrorPosition;
            return false;
        }

        value = parsed.Value;
        error = null;
        errorPosition = Position.Empty;
        return true;
    }
}