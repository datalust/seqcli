using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.PlainText.Parsers
{
    static class IdentifierEx
    {
        public static TextParser<TextSpan> CStyle { get; } =
            SpanEx.MatchedBy(
                Character.Letter.Or(Character.EqualTo('_'))
                    .IgnoreThen(Character.LetterOrDigit).Or(Character.EqualTo('_')).Many());
    }
}