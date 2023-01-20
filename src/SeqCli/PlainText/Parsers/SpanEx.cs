using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.PlainText.Parsers;

static class SpanEx
{
    public static TextParser<TextSpan> MatchedBy<T>(TextParser<T> parser)
    {
        return i =>
        {
            var result = parser(i);
                
            if (!result.HasValue)
                return Result.CastEmpty<T, TextSpan>(result);
              
            return Result.Value(
                i.Until(result.Remainder),
                i,
                result.Remainder);
        };
    }

    public static TextParser<TextSpan> NonWhiteSpace { get; } =
        Span.WithoutAny(char.IsWhiteSpace);
}