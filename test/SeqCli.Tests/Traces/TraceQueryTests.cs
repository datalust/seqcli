#nullable enable
using System;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Data;
using SeqCli.Traces;
using Serilog.Events;
using Xunit;

namespace SeqCli.Tests.Traces;

public class TraceQueryTests
{
    const string TraceId = "46a2b061ad71d2e7f93e5332012b8477";

    [Theory]
    [InlineData(TraceId, true)]
    [InlineData("46A2B061AD71D2E7F93E5332012B8477", false)]
    [InlineData("46a2b061ad71d2e7", false)]
    [InlineData("not a trace id' or true or '", false)]
    [InlineData("", false)]
    public void TraceIdsAreValidated(string traceId, bool isValid)
    {
        Assert.Equal(isValid, TraceQuery.IsValidTraceId(traceId));
    }

    [Theory]
    [InlineData("46a2b061ad71d2e7", true)]
    [InlineData("46A2B061AD71D2E7", false)]
    [InlineData(TraceId, false)]
    [InlineData("not a span id", false)]
    [InlineData("", false)]
    public void SpanIdsAreValidated(string spanId, bool isValid)
    {
        Assert.Equal(isValid, TraceQuery.IsValidSpanId(spanId));
    }

    [Fact]
    public void TheSpanOnlyQuerySelectsTheFixedColumns()
    {
        var query = TraceQuery.Build(TraceId, includeLogs: false, includeExceptions: false, []);

        Assert.Equal(
            "select @Id, @Timestamp, @Level, @StructuredMessage, @SpanId, @ParentId, @Start, @Elapsed" +
            $" from stream where @TraceId = '{TraceId}' and @Start is not null limit 10000",
            query);
    }

    [Fact]
    public void LogsExceptionsAndColumnsAppearInTheQuery()
    {
        var query = TraceQuery.Build(TraceId, includeLogs: true, includeExceptions: true,
            ["@Resource.service.name", "OrderId"]);

        Assert.Equal(
            "select @Id, @Timestamp, @Level, @StructuredMessage, @Exception, @SpanId, @ParentId, @Start, @Elapsed," +
            " @Resource.service.name as c0, OrderId as c1" +
            $" from stream where @TraceId = '{TraceId}' limit 10000",
            query);
    }

    [Fact]
    public void UnvalidatedTraceIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            TraceQuery.Build("' or true or '", includeLogs: true, includeExceptions: false, []));
    }

    [Fact]
    public void SpanRowsAreRead()
    {
        var timestamp = new DateTimeOffset(2026, 7, 31, 10, 20, 0, TimeSpan.Zero);
        var start = timestamp - TimeSpan.FromMilliseconds(1.5);

        var result = new QueryResultPart
        {
            Rows =
            [
                ["event-1", timestamp.UtcTicks, "INFO", new JArray($"Hello!{Environment.NewLine}"), null!,
                    "0011223344556677", "8899aabbccddeeff", start.UtcTicks, 15000L]
            ]
        };

        var evt = Assert.Single(TraceQuery.ReadEvents(result, includeExceptions: true, []));

        Assert.Equal("event-1", evt.Id);
        Assert.Equal(timestamp, evt.Timestamp);
        Assert.Equal("INFO", evt.Level);
        Assert.Equal("Hello!", evt.MessageTemplate.Text);
        Assert.Empty(evt.TemplateProperties);
        Assert.Null(evt.Exception);
        Assert.Equal("0011223344556677", evt.SpanId);
        Assert.Equal("8899aabbccddeeff", evt.ParentId);
        Assert.Equal(start, evt.Start);
        Assert.Equal(TimeSpan.FromMilliseconds(1.5), evt.Elapsed);
        Assert.True(evt.IsSpan);
        Assert.Equal(start, evt.SortKey);
    }

    [Fact]
    public void LogRowsAreRead()
    {
        var timestamp = new DateTimeOffset(2026, 7, 31, 10, 20, 0, TimeSpan.Zero);

        var result = new QueryResultPart
        {
            Rows = [["event-2", timestamp.UtcTicks, null!, new JArray("A log"), "System.Exception: Boom!",
                "0011223344556677", null!, null!, null!]]
        };

        var evt = Assert.Single(TraceQuery.ReadEvents(result, includeExceptions: true, []));

        Assert.Null(evt.Level);
        Assert.Equal("System.Exception: Boom!", evt.Exception);
        Assert.False(evt.IsSpan);
        Assert.Null(evt.Start);
        Assert.Null(evt.Elapsed);
        Assert.Equal(timestamp, evt.SortKey);
    }

    [Fact]
    public void ColumnsAreReadByPositionWithNullsForMissingValues()
    {
        var result = new QueryResultPart
        {
            Rows =
            [
                ["event-3", 0L, null!, JValue.CreateNull(), null!, null!, null!, null!, null!,
                    "frontend", null!, JValue.CreateNull(), 42L]
            ]
        };

        var columns = new[] {"@Resource.service.name", "Missing", "AlsoMissing", "OrderId"};

        var evt = Assert.Single(TraceQuery.ReadEvents(result, includeExceptions: true, columns));

        Assert.Equal(["frontend", null, null, 42L], evt.Columns);
    }

    [Fact]
    public void StructuredMessageHolesBecomeTemplatePropertiesAndValues()
    {
        var result = new QueryResultPart
        {
            Rows =
            [
                ["event-4", 0L, null!,
                    new JArray(
                        "Hello, ",
                        new JObject(
                            new JProperty("name", "Name"),
                            new JProperty("raw", "{Name}"),
                            new JProperty("value", "World")),
                        "!"),
                    null!, null!, null!, null!, null!]
            ]
        };

        var evt = Assert.Single(TraceQuery.ReadEvents(result, includeExceptions: true, []));

        Assert.Equal("Hello, {Name}!", evt.MessageTemplate.Text);
        var property = Assert.Single(evt.TemplateProperties);
        Assert.Equal("Name", property.Name);
        Assert.Equal(new ScalarValue("World"), property.Value);
    }

    [Fact]
    public void RowsWithoutExceptionsAreRead()
    {
        var timestamp = new DateTimeOffset(2026, 7, 31, 10, 20, 0, TimeSpan.Zero);
        var start = timestamp - TimeSpan.FromMilliseconds(1.5);

        var result = new QueryResultPart
        {
            Rows =
            [
                ["event-1", timestamp.UtcTicks, "INFO", new JArray("Hello!"),
                    "0011223344556677", "8899aabbccddeeff", start.UtcTicks, 15000L, "frontend"]
            ]
        };

        var evt = Assert.Single(TraceQuery.ReadEvents(result, includeExceptions: false, ["@Resource.service.name"]));

        Assert.Null(evt.Exception);
        Assert.Equal("0011223344556677", evt.SpanId);
        Assert.Equal("8899aabbccddeeff", evt.ParentId);
        Assert.Equal(start, evt.Start);
        Assert.Equal(TimeSpan.FromMilliseconds(1.5), evt.Elapsed);
        Assert.Equal(["frontend"], evt.Columns);
    }
}
