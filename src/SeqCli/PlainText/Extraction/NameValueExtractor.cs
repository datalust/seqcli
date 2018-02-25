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

        public TextParser<object> StartMarker => _elements[0].Parser;

        public (IDictionary<string, object>, string) ExtractValues(string plainText)
        {
            var input = new TextSpan(plainText);
            var result = new Dictionary<string, object>();
            
            var remainder = input;
            foreach (var element in _elements)
            {
                var match = element.Parser(remainder);
                if (!match.HasValue)
                {
                    if (remainder.IsAtEnd || Span.WhiteSpace.IsMatch(remainder))
                        return (result, null);

                    return (result, remainder.ToStringValue());
                }

                remainder = match.Remainder;

                if (!element.IsIgnored)
                {
                    result.Add(element.Name, match.Value);                
                }
            }

            return (result, null);
        }
    }
}
