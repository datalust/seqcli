using System;
using System.Collections.Generic;
using Superpower;
using Superpower.Model;

namespace SeqCli.PlainText.Extraction
{
    class SimplePatternElement : PatternElement
    {
        readonly TextParser<object> _parser;

        public override TextParser<Unit> Match { get; }

        public SimplePatternElement(TextParser<object> parser, string name = null)
            : base(name)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            Match = _parser.Select(s => Unit.Value);
        }

        public override bool TryExtract(
            TextSpan input,
            Dictionary<string, object> result,
            out TextSpan remainder)
        {
            var match = _parser(input);
            if (!match.HasValue)
            {
                remainder = input;
                return false;
            }

            CollectResult(result, match.Value);
            remainder = match.Remainder;

            return true;
        }
    }
}