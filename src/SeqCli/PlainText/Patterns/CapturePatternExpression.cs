using System;

namespace SeqCli.PlainText.Patterns
{
    class CapturePatternExpression : ExtractionPatternExpression
    {
        public string Name { get; }
        public string Type { get; }

        public CapturePatternExpression(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}