using System;
using Superpower;

namespace SeqCli.PlainText.Extraction
{
    class PatternElement
    {
        public PatternElement(TextParser<object> parser, string name = null)
        {
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            Name = name;
        }

        public TextParser<object> Parser { get; }
        public string Name { get; }
        public bool IsIgnored => Name == null;
    }
}