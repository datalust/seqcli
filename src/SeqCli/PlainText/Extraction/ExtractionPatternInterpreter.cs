using System;
using System.Collections.Generic;
using SeqCli.PlainText.Patterns;

namespace SeqCli.PlainText.Extraction
{
    static class ExtractionPatternInterpreter
    {
        public static NameValueExtractor MultilineMessageExtractor { get; } = new NameValueExtractor(new[]
        {
            new PatternElement(Matchers.MultiLineMessage, ReifiedProperties.Message)
        });

        public static NameValueExtractor CreateNameValueExtractor(ExtractionPattern pattern)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));

            var patternElements = new PatternElement[pattern.Elements.Count];
            var last = true;
            for (var i = pattern.Elements.Count - 1; i >= 0; --i)
            {
                var element = pattern.Elements[i];
                switch (element)
                {
                   case LiteralTextPatternExpression text:
                       patternElements[i] = new PatternElement(Matchers.LiteralText(text.Text));
                       break;
                   case CapturePatternExpression capture when capture.Type == "*":
                       if (!last)
                           patternElements[i] = new PatternElement(
                               Matchers.NonGreedyContent(patternElements[i + 1]),
                               capture.Name);
                       else
                           patternElements[i] = new PatternElement(
                               Matchers.NonGreedyContent(), // <- same as MultiLineContent
                               capture.Name);
                       break;
                   case CapturePatternExpression capture:
                       patternElements[i] = new PatternElement(
                           capture.Type == null ? Matchers.Token : Matchers.GetByType(capture.Type),
                           capture.Name);
                       break;
                   default:
                       throw new InvalidOperationException($"Element `{element}` not recognized.");
                }

                last = false;
            }
            
            return new NameValueExtractor(patternElements);
        }

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