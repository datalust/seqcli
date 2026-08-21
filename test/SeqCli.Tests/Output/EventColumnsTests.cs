#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Seq.Api.Model.Events;
using SeqCli.Api;
using SeqCli.Output;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.Output;

public class EventColumnsTests
{
    static EventColumns Create(params string[] expressions)
    {
        Assert.True(EventColumns.TryCreate(expressions, out var columns, out var error), error);
        return columns;
    }

    static string Render(EventEntity evt, EventColumns columns)
    {
        var eventJson = EventEntityJson.ToEventJson(evt);
        columns.Enrich(eventJson);

        var output = new StringWriter();
        TextFormatters.Plain(theme: null, columns.OutputTemplate()).Format(eventJson, output);
        return output.ToString();
    }

    static string At(EventEntity evt) =>
        DateTimeOffset.ParseExact(evt.Timestamp, "o", CultureInfo.InvariantCulture).ToLocalTime().ToString("o");

    [Fact]
    public void ColumnsPrecedeTheMessageInOrder()
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("Customer", "scott"), ("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] scott 42 Hello{Environment.NewLine}",
            Render(evt, Create("Customer", "OrderId")));
    }

    [Fact]
    public void MissingAndEmptyColumnValuesLeaveNoRedundantSpace()
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("Empty", ""), ("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] 42 Hello{Environment.NewLine}",
            Render(evt, Create("Missing", "Empty", "OrderId")));
    }

    [Fact]
    public void SeqStyleNamesResolveAgainstApiEvents()
    {
        var evt = Some.MakeEvent(e =>
        {
            e.Properties = [];
            e.Level = "Warning";
            e.SpanKind = "Server";
            e.Resource = Some.MakeProperties(("service.name", "frontend"));
        });

        Assert.Equal(
            $"[{At(evt)} WRN] frontend Server Hello{Environment.NewLine}",
            Render(evt, Create("@Resource['service.name']", "@SpanKind")));
    }

    [Fact]
    public void ComputedColumnValuesAreRendered()
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] order-42 Hello{Environment.NewLine}",
            Render(evt, Create("concat('order-', tostring(OrderId))")));
    }

    [Fact]
    public void InvalidExpressionsAreReportedByTryCreate()
    {
        Assert.False(EventColumns.TryCreate(new List<string> {"OrderId", "not a valid ("}, out var columns, out var error));
        Assert.Null(columns);
        Assert.NotNull(error);
        Assert.NotEmpty(error);
    }
}
