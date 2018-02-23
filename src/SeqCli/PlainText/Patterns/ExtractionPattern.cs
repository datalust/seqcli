using System;
using System.Collections.Generic;
using System.Linq;

namespace SeqCli.PlainText.Patterns
{
    class ExtractionPattern
    {
        public IReadOnlyList<ExtractionPatternExpression> Elements { get; }

        public ExtractionPattern(IEnumerable<ExtractionPatternExpression> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            Elements = items.ToArray();
        }
    }
}