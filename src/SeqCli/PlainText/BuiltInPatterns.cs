using SeqCli.PlainText.Parsers;
using Superpower;
using Superpower.Parsers;

namespace SeqCli.PlainText
{
    static class BuiltInPatterns
    {
        public static TextParser<object> Identifier { get; } =
            IdentifierEx.CStyle
                .Select(span => (object) span);
            
        public static TextParser<object> MultiLineMessage { get; } =
            SpanEx.MatchedBy(
                    Character.Matching(ch => !char.IsWhiteSpace(ch), "non whitespace character")
                        .IgnoreThen(Character.AnyChar.Many()))
                .Select(span => (object)span);

        public static TextParser<object> LiteralText(string literalText)
        {
            return Span.EqualTo(literalText).Select(span => (object) span);
        }
    }
}
