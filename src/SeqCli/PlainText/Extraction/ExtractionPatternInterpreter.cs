using System;
using System.Collections.Generic;
using System.Linq;
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
            for (var i = pattern.Elements.Count - 1; i >= 0; --i)
            {
                var element = pattern.Elements[i];
                switch (element)
                {
                   case LiteralTextPatternExpression text:
                       patternElements[i] = new PatternElement(Matchers.LiteralText(text.Text));
                       break;
                   case CapturePatternExpression capture
                       when capture.Content is NonGreedyContentExpression ngc:
                       patternElements[i] = new PatternElement(
                           Matchers.NonGreedyContent(patternElements.Skip(i + 1).Take(ngc.Lookahead).ToArray()),
                           capture.Name);
                       break;
                   case CapturePatternExpression capture
                       when capture.Content is MatchTypeContentExpression mtc:
                       patternElements[i] = new PatternElement(
                           mtc.Type == null ? Matchers.Token : Matchers.GetByType(mtc.Type),
                           capture.Name);
                       break;
                   default:
                       throw new InvalidOperationException($"Element `{element}` not recognized.");
                }
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