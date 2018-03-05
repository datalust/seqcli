using Superpower;
using Superpower.Parsers;

namespace SeqCli.PlainText.Parsers
{
    static class StringsEx
    {
        static readonly TextParser<char> SqlStringContentChar =
            Span.EqualTo("''").Value('\'').Try().Or(Character.ExceptIn('\'', '\r', '\n'));

        public static TextParser<string> SqlStyle { get; } =
            Character.EqualTo('\'')
                .IgnoreThen(SqlStringContentChar.Many())
                .Then(s => Character.EqualTo('\'').Value(new string(s)));
    }
}
