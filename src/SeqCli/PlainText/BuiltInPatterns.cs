using SeqCli.PlainText.Parsers;
using Superpower;
using Superpower.Model;
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
        
        public static TextParser<object> MultiLineContent { get; } =
            SpanEx.MatchedBy(Character.AnyChar.Many())
                .Select(span => (object)span);
        
        public static TextParser<object> SingleLineContent { get; } =
            SpanEx.MatchedBy(Character.ExceptIn('\r', '\n').Many())
                .Select(span => (object)span);

        public static TextParser<object> LiteralText(string literalText)
        {
            return Span.EqualTo(literalText).Select(span => (object) span);
        }

        public static TextParser<object> NonGreedyContent(PatternElement[] following)
        {
            if (following.Length == 0)
                return SpanEx.MatchedBy(Character.AnyChar.Many()).Select(span => (object) span);

            var rest = following[0].Parser;
            for (var i = 1; i < following.Length; ++i)
            {
                rest = rest.IgnoreThen(following[i].Parser);
            }

            return i =>
            {
                var remainder = i;
                while (!rest.IsMatch(remainder))
                {
                    remainder = remainder.ConsumeChar().Remainder;
                }

                var span = i.Until(remainder);
                return Result.Value((object) span, i, remainder);
            };
        }
    }
}
