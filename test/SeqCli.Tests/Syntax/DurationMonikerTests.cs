using System;
using SeqCli.Syntax;
using Xunit;

namespace SeqCli.Tests.Syntax;

public class DurationMonikerTests
{
    [Theory]
    [InlineData("1d", 1000L * 60 * 60 * 24)]
    [InlineData("7d", 1000L * 60 * 60 * 24 * 7)]
    [InlineData("30ms", 30L)]
    public void MonikersAreParsedAndFormatted(string duration, long equivalentMilliseconds)
    {
        var ts = DurationMoniker.ToTimeSpan(duration);
        Assert.Equal(TimeSpan.FromMilliseconds(equivalentMilliseconds), ts);

        var dm = DurationMoniker.FromTimeSpan(ts);
        Assert.Equal(duration, dm);
    }
}