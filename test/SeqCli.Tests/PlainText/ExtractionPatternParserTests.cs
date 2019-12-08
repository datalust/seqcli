using System;
using System.Linq;
using SeqCli.PlainText.Patterns;
using Superpower;
using Xunit;

namespace SeqCli.Tests.PlainText
{
    public class ExtractionPatternParserTests
    {
        [Fact]
        public void ARegularStringIsASingleTextLiteral()
        {
            var pattern = ExtractionPatternParser.Parse("Hello!");
            Assert.Single(pattern.Elements);
            var tt = Assert.IsType<LiteralTextPatternExpression>(pattern.Elements.Single());
            Assert.Equal("Hello!", tt.Text);
        }
        
        [Fact]
        public void CaptureNameAndTypeAreParsed()
        {
            var pattern = ExtractionPatternParser.Parse("{abc:def}");
            Assert.Single(pattern.Elements);
            var ct = Assert.IsType<CapturePatternExpression>(pattern.Elements.Single());
            Assert.Equal("abc", ct.Name);
            Assert.Equal("def", ((MatchTypeContentExpression)ct.Content).Type);
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("{}", false)]
        [InlineData("{a", false)]
        [InlineData("a", true)]
        [InlineData("{a}", true)]
        [InlineData("{@m}", true)]
        [InlineData("{@@m}", false)]
        [InlineData("{m@}", false)]
        [InlineData("{@m:n}", true)]
        [InlineData("{@m:*}", true)]
        [InlineData("{@m:***}", true)]
        [InlineData("{:*}", true)]
        [InlineData("{a:}", false)]
        [InlineData("{m_N}", true)]
        [InlineData("{_9}", true)]
        [InlineData("{:n}", true)]
        [InlineData("{:}", false)]
        [InlineData("{{@m}}", true)]
        [InlineData("{{a", true)]
        [InlineData("a}}", true)]
        [InlineData("{", false)]
        [InlineData("}", false)]
        [InlineData("{a} b{c} ", true)]
        [InlineData("d {a}b {c}", true)]
        [InlineData("{:={@m}}", true)]
        [InlineData("Loaded {SignalId:=signal-{:nat}}", true)]
        [InlineData("{:={Year:num}-{Month:num}}", true)]
        public void OnlyValidPatternsAreAccepted(string attempt, bool isValid)
        {
            if (isValid)
                ExtractionPatternParser.Parse(attempt);
            else
                Assert.Throws<ParseException>(() => ExtractionPatternParser.Parse(attempt));
        }
    }
}