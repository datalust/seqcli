namespace SeqCli.PlainText.Patterns
{
    class CapturePatternExpression : ExtractionPatternExpression
    {
        public string Name { get; }
        public CaptureContentExpression Content { get; }

        public CapturePatternExpression(string name, CaptureContentExpression content)
        {
            Name = name;
            Content = content;
        }
    }
}
