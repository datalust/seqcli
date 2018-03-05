using System;
using System.Collections.Generic;
using System.Linq;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace SeqCli.PlainText.Extraction
{
    class NameValueExtractor
    {
        readonly PatternElement[] _elements;

        public NameValueExtractor(IEnumerable<PatternElement> elements)
        {
            _elements = elements?.ToArray() ?? throw new ArgumentNullException(nameof(elements));
            if (_elements.Length == 0)
                throw new ArgumentException("An extraction pattern must contain at least one element.");            
        }

        public TextParser<Unit> StartMarker => _elements[0].Match;

        public (IDictionary<string, object>, string) ExtractValues(string plainText)
        {
            var input = new TextSpan(plainText);
            var result = new Dictionary<string, object>();
            
            var remainder = input;
            foreach (var element in _elements)
            {
                if (!element.TryExtract(remainder, result, out remainder))
                {
                    if (remainder.IsAtEnd || Span.WhiteSpace.IsMatch(remainder))
                        return (result, null);

                    return (result, remainder.ToStringValue());
                }
            }

            return (result, null);
        }
    }
}
