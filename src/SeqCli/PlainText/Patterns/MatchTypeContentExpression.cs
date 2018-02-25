namespace SeqCli.PlainText.Patterns
{
    class MatchTypeContentExpression : CaptureContentExpression
    {
        public string Type { get; }

        public MatchTypeContentExpression(string type)
        {
            Type = type;
        }
    }
}