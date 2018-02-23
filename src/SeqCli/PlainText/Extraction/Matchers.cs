using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SeqCli.PlainText.Parsers;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.PlainText.Extraction
{
    // ReSharper disable UnusedMember.Global
    static class Matchers
    {
        [Matcher("ident")]
        public static TextParser<object> Identifier { get; } =
            IdentifierEx.CStyle
                .Select(span => (object) span);
        
        [Matcher("nat")]
        public static TextParser<object> Natural { get; } =
            Numerics.NaturalUInt64
                .Select(span => (object) span);
        
        [Matcher("int")]
        public static TextParser<object> Integer { get; } =
            Numerics.IntegerInt64
                .Select(span => (object) span);
        
        [Matcher("dec")]
        public static TextParser<object> Decimal { get; } =
            NumericsEx.Decimal
                .Select(span => (object) span);
        
        [Matcher("alpha")]
        public static TextParser<object> Alphabetical { get; } =
            Span.WithAll(char.IsLetter)
                .Select(span => (object) span);
        
        [Matcher("alphanum")]
        public static TextParser<object> Alphanumeric { get; } =
            Span.WithAll(char.IsLetterOrDigit)
                .Select(span => (object) span);
        
        [Matcher("token")]
        public static TextParser<object> Token { get; } =
            SpanEx.NonWhiteSpace.Select(span => (object)span);

        // Unclear whether we need to name this
        public static TextParser<object> MultiLineMessage { get; } =
            SpanEx.MatchedBy(
                    Character.Matching(ch => !char.IsWhiteSpace(ch), "non whitespace character")
                        .IgnoreThen(Character.AnyChar.Many()))
                .Select(span => (object)span);

        [Matcher("lines")]
        public static TextParser<object> MultiLineContent { get; } =
            Span.WithAll(ch => true)
                .Select(span => (object)span);

        [Matcher("line")]
        public static TextParser<object> SingleLineContent { get; } =
            from content in Span.WithoutAny(ch => ch == '\r' || ch == '\n')
            from _ in NewLine.OptionalOrDefault()
            select (object) content;

        [Matcher("n")]
        public static TextParser<object> NewLine { get; } =
            Span.EqualTo("\r\n").Or(Span.EqualTo("\n"))
                .Select(span => (object)span);

        static readonly Dictionary<string, TextParser<object>> ByType = new Dictionary<string, TextParser<object>>(
            from pi in typeof(Matchers).GetTypeInfo().DeclaredProperties
            let attr = pi.GetCustomAttribute<MatcherAttribute>()
            where attr != null
            select KeyValuePair.Create(attr.Name, (TextParser<object>) pi.GetValue(null)));

        public static TextParser<object> GetByType(string type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return ByType[type];
        }

        public static TextParser<object> LiteralText(string literalText)
        {
            return Span.EqualTo(literalText).Select(span => (object) span);
        }

        public static TextParser<object> NonGreedyContent(params PatternElement[] following)
        {
            if (following.Length == 0)
                return SpanEx.MatchedBy(Character.AnyChar.Many())
                    .Select(span => span.Length > 0 ? (object) span : null);

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
                return Result.Value(span.Length > 0 ? (object) span : null, i, remainder);
            };
        }
    }
}
