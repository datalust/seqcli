using System;
using System.Globalization;
using SeqCli.PlainText.Extraction;
using Superpower;
using Superpower.Model;
using Xunit;

namespace SeqCli.Tests.PlainText
{
    public class MatchersTests
    {
        [Theory]
        [InlineData("Mar  6 13:22:29", 3, 6, 13, 22, 29)]
        [InlineData("Dec 31 23:59:59", 12, 31, 23, 59, 59)]
        [InlineData("Jan  1 00:00:00", 1, 1, 0, 0, 0)]
        public void DefaultSyslogDateTimeIsMatched(string timestamp, int month, int day, int hour, int minute, int second)
        {
            var result = Matchers.SyslogDefaultTimestamp.AtEnd().Parse(timestamp);
            var dto = Assert.IsType<DateTimeOffset>(result);
            Assert.Equal(month, dto.Month);
            Assert.Equal(day, dto.Day);
            Assert.Equal(hour, dto.Hour);
            Assert.Equal(minute, dto.Minute);
            Assert.Equal(second, dto.Second);
            Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(dto), dto.Offset);
        }

        [Fact]
        public void TimestampMatcherCorrectlyExcludesTrailingWhitespace()
        {
            var timestamp = "2019-03-26 21:48:26 xxx";
            var result = Matchers.Timestamp.Parse(timestamp);
            Assert.Equal(DateTimeOffset.Parse("2019-03-26 21:48:26", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal), result);
        }

        [Fact]
        public void UnixDtMatcherProducesUtcTimestamps()
        {
            var timestamp = "1608694199";
            var result = Matchers.UnixTimestamp.Parse(timestamp);
            Assert.Equal(DateTimeOffset.Parse("2020-12-23T03:29:59.0000000+00:00"), result);
        }

        [Fact]
        public void UnixDtWithFractionalSecondsMatcherProducesUtcTimestamps()
        {
            var timestamp = "1608694199.019";
            var result = Matchers.UnixTimestamp.Parse(timestamp);
            Assert.Equal(DateTimeOffset.Parse("2020-12-23T03:29:59.0190000+00:00"), result);
        }

        [Fact]
        public void UnixMsDtMatcherProducesUtcTimestamps()
        {
            var timestamp = "1608707704559";
            var result = Matchers.UnixTimestamp.Parse(timestamp);
            Assert.Equal(DateTimeOffset.Parse("2020-12-23T07:15:04.5590000+00:00"), result);
        }

        [Fact]
        public void W3CMatcherProducesUtcTimestamps()
        {
            var timestamp = "2019-03-26 21:48:26 xxx";
            var result = Matchers.W3CTimestamp.Parse(timestamp);
            Assert.Equal(DateTimeOffset.Parse("2019-03-26 21:48:26 Z"), result);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("       ")]
        public void WhiteSpaceMatcherEatsOneOrMoreSpaces(string space)
        {
            var input = new TextSpan(space + "x");
            var s = Matchers.WhiteSpace(input);
            Assert.Equal(space, ((TextSpan)s.Value).ToStringValue());
            Assert.Equal('x', s.Remainder.ConsumeChar().Value);
        }

        // Zero-length whitespace is not supported as it's then incompatible
        // with `*`.
        [Fact]
        public void WhiteSpaceMatcherDoesNotSucceedOnZeroLength()
        {
            var input = new TextSpan("x");
            var s = Matchers.WhiteSpace(input);
            Assert.False(s.HasValue);
        }
    }
}