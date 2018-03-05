using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.PlainText.Parsers
{
    static class DateTimesEx
    {
        public static TextParser<TextSpan> Iso8601DateTime { get; } =
            Span.Regex("\\d{4}-\\d\\d-\\d\\dT\\d\\d:\\d\\d:\\d\\d(\\.\\d+)?(([+-]\\d\\d:\\d\\d)|Z)?");
    }
}
