using System;
using SeqCli.PlainText.Extraction;
using Superpower;
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
            Assert.Equal(DateTimeOffset.Parse("2019-03-26 21:48:26 +10:00"), result);
        }

        [Fact]
        public void W3CMatcherProducesUtcTimestamps()
        {
            var timestamp = "2019-03-26 21:48:26 xxx";
            var result = Matchers.W3CTimestamp.Parse(timestamp);
            Assert.Equal(DateTimeOffset.Parse("2019-03-26 21:48:26 Z"), result);
        }
    }
}