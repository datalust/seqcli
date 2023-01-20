using System;

namespace SeqCli.PlainText.Patterns;

class LiteralTextPatternExpression : ExtractionPatternExpression
{
    public string Text { get; }

    public LiteralTextPatternExpression(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}