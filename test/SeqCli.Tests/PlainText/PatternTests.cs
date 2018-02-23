using System;
using System.Linq;
using SeqCli.PlainText;
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
            var (properties, remainder) = PatternBuilder.DefaultPattern.Match(frame);
            Assert.Null(remainder);
            Assert.Single(properties, p => p.Key == ReifiedProperties.Message &&
                                           ((TextSpan)p.Value).ToStringValue() == frame);
        }

        [Fact]
        public void TheDefaultPatternDoesNotMatchLinesStartingWithWhitespace()
        {
            var frame = " world";
            var (properties, remainder) = PatternBuilder.DefaultPattern.Match(frame);
            Assert.Empty(properties);
            Assert.Equal(frame, remainder);
        }

        static Pattern ClassMethodPattern { get; } = new Pattern(new[]
        {
            new PatternElement(BuiltInPatterns.Identifier, "class"),
            new PatternElement(BuiltInPatterns.LiteralText(".")),
            new PatternElement(BuiltInPatterns.Identifier, "method")
        });

        [Fact]
        public void PatternsExtractElements()
        {
            var pattern = ClassMethodPattern;

            var frame = "this.that";
            var (properties, remainder) = pattern.Match(frame);
            Assert.Null(remainder);
            Assert.Equal("this", properties["class"].ToString());
            Assert.Equal("that", properties["method"].ToString());
        }

        [Fact]
        public void TheFirstPatternElementIsExposed()
        {
            Assert.Same(BuiltInPatterns.Identifier, ClassMethodPattern.FrameStart);
        }

        [Fact]
        public void SingleLineContentMatchesUntilEol()
        {
            var pattern = new Pattern(new[]
            {
                new PatternElement(BuiltInPatterns.Identifier, "first"),
                new PatternElement(BuiltInPatterns.LiteralText(" ")),
                new PatternElement(BuiltInPatterns.SingleLineContent, "content"), 
                new PatternElement(BuiltInPatterns.LiteralText(" (")),
                new PatternElement(BuiltInPatterns.Identifier, "last"),
                new PatternElement(BuiltInPatterns.LiteralText(")"))
            });

            var frame = "abc def ghi (jkl)";
            var (properties, remainder) = pattern.Match(frame);
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
                new PatternElement(BuiltInPatterns.LiteralText(" (")),
                new PatternElement(BuiltInPatterns.Identifier, "last"),
                new PatternElement(BuiltInPatterns.LiteralText(")"))
            };
            
            var pattern = new Pattern(new[]
            {
                new PatternElement(BuiltInPatterns.Identifier, "first"),
                new PatternElement(BuiltInPatterns.LiteralText(" ")),
                new PatternElement(BuiltInPatterns.NonGreedyContent(following), "content"), 
            }.Concat(following));

            var frame = "abc def ghi (jkl)";
            var (properties, remainder) = pattern.Match(frame);
            Assert.Null(remainder);
            Assert.Equal("abc", properties["first"].ToString());
            Assert.Equal("def ghi", properties["content"].ToString());
            Assert.Equal("jkl", properties["last"].ToString());
        }
    }
}