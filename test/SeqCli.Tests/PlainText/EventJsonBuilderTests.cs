#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using SeqCli.Data;
using SeqCli.PlainText;
using Superpower.Model;
using Xunit;

namespace SeqCli.Tests.PlainText;

public class EventJsonBuilderTests
{
    [Fact]
    public void SuppliedValuesAreUsed()
    {
        var properties = new Dictionary<string, object?>
        {
            ["@t"] = new TextSpan("2018-02-01T13:00:00.123Z"),
            ["@l"] = new TextSpan("WRN"),
            ["@m"] = new TextSpan("Hello, world"),
            ["@x"] = new TextSpan("EverythingFailedException"),
            ["MachineName"] = new TextSpan("TP"),
            ["Count"] = 42
        };

        var remainder = "rem";
        var evt = EventJsonBuilder.FromProperties(properties, remainder);

        Assert.Equal("2018-02-01T13:00:00.1230000+00:00",
            DateTimeOffset.Parse((string)evt["@t"]!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                .ToUniversalTime().ToString("o"));
        Assert.Equal("Hello, world", (string?)evt["@m"]);
        Assert.Equal("WRN", (string?)evt["@l"]);
        Assert.Equal("EverythingFailedException", (string?)evt["@x"]);
        Assert.Equal(42, (int?)evt["Count"]);
        Assert.Equal("TP", (string?)evt["MachineName"]);
        Assert.Equal("rem", (string?)evt["@@unmatched"]);
    }

    [Fact]
    public void MissingValuesAreDefaulted()
    {
        var evt = EventJsonBuilder.FromProperties(new Dictionary<string, object?>(), null);

        var timestamp = DateTimeOffset.Parse((string)evt["@t"]!, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
        Assert.True(timestamp > DateTimeOffset.Now.AddSeconds(-5));
        Assert.False(evt.ContainsKey("@m"));
        Assert.False(evt.ContainsKey("@l"));
        Assert.False(evt.ContainsKey("@x"));
    }

    [Fact]
    public void DateTimeOffsetTimestampsAreAccepted()
    {
        var then = DateTimeOffset.Now.AddDays(-5);
        var evt = EventJsonBuilder.FromProperties(new Dictionary<string, object?>{["@t"] = then}, null);
        Assert.Equal(then.ToString("o", CultureInfo.InvariantCulture), (string?)evt["@t"]);
    }
}
