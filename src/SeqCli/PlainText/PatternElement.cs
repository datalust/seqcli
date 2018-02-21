using System;
using Superpower;

namespace SeqCli.PlainText
{
    class PatternElement
    {
        public PatternElement(TextParser<object> parser, string name = null, bool isOptional = false)
        {
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            Name = name;
            IsOptional = isOptional;
        }

        public TextParser<object> Parser { get; }
        public string Name { get; }
        public bool IsOptional { get; }
        public bool IsIgnored => Name == null;
    }
}