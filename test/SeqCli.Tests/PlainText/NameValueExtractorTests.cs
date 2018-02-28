using System;
using System.Linq;
using SeqCli.PlainText;
using SeqCli.PlainText.Extraction;
using Superpower.Model;
using Xunit;

namespace SeqCli.Tests.PlainText
{
    public class NameValueExtractorTests
    {
        [Fact]
        public void TheDefaultPatternMatchesMultilineMessages()
        {
            var frame = $"Hello,{Environment.NewLine} world!";
            var (properties, remainder) = ExtractionPatternInterpreter.MultilineMessageExtractor.ExtractValues(frame);
            Assert.Null(remainder);
            Assert.Single(properties, p => p.Key == ReifiedProperties.Message &&
                                           ((TextSpan)p.Value).ToStringValue() == frame);
        }

        [Fact]
        public void TheDefaultPatternDoesNotMatchLinesStartingWithWhitespace()
        {
            var frame = " world";
            var (properties, remainder) = ExtractionPatternInterpreter.MultilineMessageExtractor.ExtractValues(frame);
            Assert.Empty(properties);
            Assert.Equal(frame, remainder);
        }

        static NameValueExtractor ClassMethodPattern { get; } = new NameValueExtractor(new[]
        {
            new SimplePatternElement(Matchers.Identifier, "class"),
            new SimplePatternElement(Matchers.LiteralText(".")),
            new SimplePatternElement(Matchers.Identifier, "method")
        });

        [Fact]
        public void PatternsExtractElements()
        {
            var pattern = ClassMethodPattern;

            var frame = "this.that";
            var (properties, remainder) = pattern.ExtractValues(frame);
            Assert.Null(remainder);
            Assert.Equal("this", properties["class"].ToString());
            Assert.Equal("that", properties["method"].ToString());
        }

        [Fact]
        public void TheFirstPatternElementIsExposed()
        {
            Assert.NotNull(ClassMethodPattern.StartMarker);
        }

        [Fact]
        public void SingleLineContentMatchesUntilEol()
        {
            var pattern = new NameValueExtractor(new[]
            {
                new SimplePatternElement(Matchers.Identifier, "first"),
                new SimplePatternElement(Matchers.LiteralText(" ")),
                new SimplePatternElement(Matchers.SingleLineContent, "content"), 
                new SimplePatternElement(Matchers.LiteralText(" (")),
                new SimplePatternElement(Matchers.Identifier, "last"),
                new SimplePatternElement(Matchers.LiteralText(")"))
            });

            var frame = "abc def ghi (jkl)";
            var (properties, remainder) = pattern.ExtractValues(frame);
            Assert.Null(remainder);
            Assert.Equal("abc", properties["first"].ToString());
            Assert.Equal("def ghi (jkl)", properties["content"].ToString());
        }
        
        [Fact]
        public void NonGreedyContentStopsMatchingWhenFollowingTokensMatch()
        {
            // It's likely we'll only be able to get one or two tokens into
            // the "following" list, since they effectively become "mandatory"
            var following = new[]
            {
                new SimplePatternElement(Matchers.LiteralText(" (")),
                new SimplePatternElement(Matchers.Identifier, "last"),
                new SimplePatternElement(Matchers.LiteralText(")"))
            };
            
            var pattern = new NameValueExtractor(new[]
            {
                new SimplePatternElement(Matchers.Identifier, "first"),
                new SimplePatternElement(Matchers.LiteralText(" ")),
                new SimplePatternElement(Matchers.NonGreedyContent(following), "content"), 
            }.Concat(following));

            var frame = "abc def ghi (jkl)";
            var (properties, remainder) = pattern.ExtractValues(frame);
            Assert.Null(remainder);
            Assert.Equal("abc", properties["first"].ToString());
            Assert.Equal("def ghi", properties["content"].ToString());
            Assert.Equal("jkl", properties["last"].ToString());
        }
    }
}