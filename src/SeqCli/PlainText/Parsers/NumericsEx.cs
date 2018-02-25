using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.PlainText.Parsers
{
    static class NumericsEx
    {
        public static TextParser<TextSpan> Decimal { get; } =
            Numerics.Integer
                .Then(n => Character.EqualTo('.').IgnoreThen(Numerics.Integer).OptionalOrDefault()
                    .Select(f => f == TextSpan.None ? n : new TextSpan(n.Source, n.Position, n.Length + f.Length + 1)));

        public static TextParser<TextSpan> HexNatural { get; } =
            SpanEx.MatchedBy(Span.EqualTo("0x")
                .IgnoreThen(Character.Digit
                    .Or(Character.Matching(ch => ch >= 'a' && ch <= 'f' || ch >= 'A' && ch <= 'F', "a-f"))
                    .Named("hex digit")
                    .AtLeastOnce()));
    }
}