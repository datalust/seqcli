using Superpower;
using Superpower.Model;

namespace SeqCli.PlainText.Parsers
{
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
    }
}