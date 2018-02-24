namespace SeqCli.PlainText.Patterns
{
    class NonGreedyContentExpression : CaptureContentExpression
    {
        public int Lookahead { get; }

        public NonGreedyContentExpression(int lookahead)
        {
            Lookahead = lookahead;
        }
    }
}