using System;
using System.Linq;
using SeqCli.PlainText;
using SeqCli.PlainText.Extraction;
using Superpower.Model;
using Xunit;

namespace SeqCli.Tests.PlainText
{
    public class PatternTests
    {
        [Fact]
        public void TheDefaultPatternMatchesMultilineMessages()
        {
            var frame = $"Hello,{Environment.NewLine} world!";
            var (properties, remainder) = PatternCompiler.MultilineMessageExtractor.ExtractValues(frame);
            Assert.Null(remainder);
            Assert.Single(properties, p => p.Key == ReifiedProperties.Message &&
                                           ((TextSpan)p.Value).ToStringValue() == frame);
        }

        [Fact]
        public void TheDefaultPatternDoesNotMatchLinesStartingWithWhitespace()
        {
            var frame = " world";
            var (properties, remainder) = PatternCompiler.MultilineMessageExtractor.ExtractValues(frame);
            Assert.Empty(properties);
            Assert.Equal(frame, remainder);
        }

        static NameValueExtractor ClassMethodPattern { get; } = new NameValueExtractor(new[]
        {
            new PatternElement(Matchers.Identifier, "class"),
            new PatternElement(Matchers.LiteralText(".")),
            new PatternElement(Matchers.Identifier, "method")
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
            Assert.Same(Matchers.Identifier, ClassMethodPattern.StartMarker);
        }

        [Fact]
        public void SingleLineContentMatchesUntilEol()
        {
            var pattern = new NameValueExtractor(new[]
            {
                new PatternElement(Matchers.Identifier, "first"),
                new PatternElement(Matchers.LiteralText(" ")),
                new PatternElement(Matchers.SingleLineContent, "content"), 
                new PatternElement(Matchers.LiteralText(" (")),
                new PatternElement(Matchers.Identifier, "last"),
                new PatternElement(Matchers.LiteralText(")"))
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
                new PatternElement(Matchers.LiteralText(" (")),
                new PatternElement(Matchers.Identifier, "last"),
                new PatternElement(Matchers.LiteralText(")"))
            };
            
            var pattern = new NameValueExtractor(new[]
            {
                new PatternElement(Matchers.Identifier, "first"),
                new PatternElement(Matchers.LiteralText(" ")),
                new PatternElement(Matchers.NonGreedyContent(following), "content"), 
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