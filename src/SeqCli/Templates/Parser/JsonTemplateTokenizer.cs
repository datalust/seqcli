using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace SeqCli.Templates.Parser
{
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
}
