namespace SeqCli.PlainText
{
    static class PatternBuilder
    {
        public static Pattern DefaultPattern = new Pattern(new[] {
                new PatternElement(BuiltInPatterns.MultiLineMessage, ReifiedProperties.Message)
            });
    }
}