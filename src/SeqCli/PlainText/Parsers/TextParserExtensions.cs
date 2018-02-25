using Superpower;

namespace SeqCli.PlainText.Parsers
{
    public static class TextParserExtensions
    {
        public static TextParser<U> Cast<T, U>(this TextParser<T> parser)
            where T : U
        {
            return parser.Select(t => (U) t);
        }
    }
}