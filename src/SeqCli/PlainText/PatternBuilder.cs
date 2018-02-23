namespace SeqCli.PlainText
{
    static class PatternBuilder
    {
        public static Pattern DefaultPattern { get; } = new Pattern(new[] {
                new PatternElement(BuiltInPatterns.MultiLineMessage, ReifiedProperties.Message)
            });
        
        // What we need to do here is:
        //  - for each parsed token
        //    - if it's literal text, map it an anonymous PatternElement with
        //       BuiltInPatterns.LiteralText()
        //    - otherwise, if it specifies no format, it's a named element with
        //       the BuiltInPatterns.Token parser
        //    - if it does specify a format, look up the parser based on the name, except
        //    - if the format is `$` it is BuiltInPatterns.SingleLineContent
        //    - if the format is `$$` it is BuiltInPatterns.MultiLineContent
        //    - if it's `*`, it's BuiltInPatterns.NonGreedyContent() passing the
        //       parser that follows it
    }
}