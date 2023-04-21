using System;
using System.Collections.Generic;
using System.Linq;
using SeqCli.Levels;
using SeqCli.PlainText.LogEvents;
using Serilog.Events;
using Superpower.Model;
using Xunit;

namespace SeqCli.Tests.PlainText;

public class LogEventBuilderTests
{
    [Fact]
    public void SuppliedValuesAreUsed()
    {
        var properties = new Dictionary<string, object>
        {
            ["@t"] = new TextSpan("2018-02-01T13:00:00.123Z"),
            ["@l"] = new TextSpan("WRN"),
            ["@m"] = new TextSpan("Hello, world"),
            ["@x"] = new TextSpan("EverythingFailedException"),
            ["MachineName"] = new TextSpan("TP"),
            ["Count"] = 42
        };

        var remainder = "rem";
        var evt = LogEventBuilder.FromProperties(properties, remainder);

        Assert.Equal("2018-02-01T13:00:00.1230000+00:00", evt.Timestamp.ToString("o"));
        Assert.Equal("Hello, world", evt.RenderMessage());
        Assert.Equal(LogEventLevel.Warning, evt.Level);
        Assert.Equal("WRN", ((ScalarValue)evt.Properties[SurrogateLevelProperty.PropertyName]).Value);
        Assert.Equal("EverythingFailedException", evt.Exception?.ToString());
        Assert.Equal(42, ((ScalarValue)evt.Properties["Count"]).Value);
        Assert.Equal("TP", ((ScalarValue)evt.Properties["MachineName"]).Value!.ToString());
        Assert.Equal("rem", ((ScalarValue)evt.Properties["@unmatched"]).Value!.ToString());
    }

    [Fact]
    public void MissingValuesAreDefaulted()
    {
        var evt = LogEventBuilder.FromProperties(new Dictionary<string, object>(), null);

        Assert.True(evt.Timestamp > DateTimeOffset.Now.AddSeconds(-5));
        Assert.Equal("", evt.RenderMessage());
        Assert.Equal(LogEventLevel.Information, evt.Level);
        Assert.Null(evt.Exception);
        Assert.Equal(SurrogateLevelProperty.PropertyName, evt.Properties.Single().Key);
    }

    [Fact]
    public void DateTimeOffsetTimestampsAreAccepted()
    {
        var then = DateTimeOffset.Now.AddDays(-5);
        var evt = LogEventBuilder.FromProperties(new Dictionary<string, object>{["@t"] = then}, null);
        Assert.Equal(then, evt.Timestamp);
    }
}