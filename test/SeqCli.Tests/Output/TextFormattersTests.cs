#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using Seq.Api.Model.Events;
using Seq.Syntax.Templates.Themes;
using SeqCli.Api;
using SeqCli.Output;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.Output;

public class TextFormattersTests
{
    const char Escape = '\x1b';
    const string FixedTimestamp = "2024-01-01T10:00:01.2500000+00:00";

    [Fact]
    public void ThemedJsonOutputIsColorizedRegardlessOfRedirection()
    {
        Assert.Contains(Escape, RenderJson(TemplateTheme.Code));
    }

    [Fact]
    public void UnthemedJsonOutputIsNotColorized()
    {
        Assert.DoesNotContain(Escape, RenderJson(theme: null));
    }

    [Fact]
    public void UnthemedJsonOutputIsTheEventDocumentVerbatim()
    {
        Assert.Equal(
            """{"@t":"2024-01-01T10:00:01.2500000+00:00","@mt":"Hello, {Name}!","Name":"world"}""" + Environment.NewLine,
            RenderJson(theme: null, SomeEventJson()));
    }

    [Fact]
    public void LogEventsAreFormattedWithTheDefaultTextTemplate()
    {
        var evt = SomeEventJson(level: "Warning");

        Assert.Equal(
            $"[2024-01-01T10:00:01.2500000+00:00 WRN] Hello, world!{Environment.NewLine}",
            RenderText(evt));
    }

    [Fact]
    public void ExceptionsAreIncludedInTextOutput()
    {
        var evt = SomeEventJson(level: "Error", exception: "System.Exception: Boom!");

        Assert.Equal(
            $"[2024-01-01T10:00:01.2500000+00:00 ERR] Hello, world!{Environment.NewLine}System.Exception: Boom!{Environment.NewLine}",
            RenderText(evt));
    }

    [Fact]
    public void SpanElapsedTimeIsComputedFromTheStartTimestamp()
    {
        var evt = SomeEventJson();
        evt["@st"] = "2024-01-01T10:00:00.0000000Z";

        Assert.Equal(
            $"[2024-01-01T10:00:01.2500000+00:00 INF] Hello, world! (1250 ms){Environment.NewLine}",
            RenderText(evt));
    }

    [Fact]
    public void ACustomOutputTemplateReplacesTheDefault()
    {
        Assert.Equal(
            $"INF Hello, world!{Environment.NewLine}",
            RenderText(SomeEventJson(), $"{{@l:u3}} {{@m}}{Environment.NewLine}"));
    }

    [Fact]
    public void ColumnsPrecedeTheMessageInOrder()
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("Customer", "scott"), ("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] scott 42 Hello{Environment.NewLine}",
            RenderText(evt, "Customer", "OrderId"));
    }

    [Fact]
    public void MissingAndEmptyColumnValuesLeaveNoRedundantSpace()
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("Empty", ""), ("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] 42 Hello{Environment.NewLine}",
            RenderText(evt, "Missing", "Empty", "OrderId"));
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
            RenderText(evt, "@Resource['service.name']", "@SpanKind"));
    }

    [Fact]
    public void ComputedColumnValuesAreRendered()
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] order-42 Hello{Environment.NewLine}",
            RenderText(evt, "concat('order-', tostring(OrderId))"));
    }

    [Theory]
    [InlineData("if OrderId > 40 then 'big' else 'small'", "big")]
    [InlineData("{id: OrderId}.id", "42")]
    [InlineData("concat('{', tostring(OrderId), '}')", "{42}")]
    [InlineData("Missing or OrderId = 42", "true")]
    [InlineData("[OrderId, 'x'][0]", "42")]
    public void ColumnExpressionsUsingTemplateDelimitersAreRendered(string column, string expected)
    {
        var evt = Some.MakeEvent(e => e.Properties = Some.MakeProperties(("OrderId", 42)));

        Assert.Equal(
            $"[{At(evt)} INF] {expected} Hello{Environment.NewLine}",
            RenderText(evt, column));
    }

    static string At(EventEntity evt) =>
        DateTimeOffset.ParseExact(evt.Timestamp, "o", CultureInfo.InvariantCulture).ToLocalTime().ToString("o");

    static string RenderText(EventEntity evt, params string[] columns) =>
        RenderText(EventEntityJson.ToEventJson(evt), TextFormatters.PlainOutputTemplate(columns));

    static JsonObject SomeEventJson(string? level = null, string? exception = null)
    {
        var evt = new JsonObject
        {
            ["@t"] = FixedTimestamp,
            ["@mt"] = "Hello, {Name}!",
            ["Name"] = "world"
        };

        if (level != null)
            evt["@l"] = level;

        if (exception != null)
            evt["@x"] = exception;

        return evt;
    }

    static string RenderText(JsonObject evt, string? outputTemplate = null)
    {
        var output = new StringWriter();
        TextFormatters.Plain(theme: null, outputTemplate).Format(evt, output);
        return output.ToString();
    }

    static string RenderJson(TemplateTheme? theme, JsonObject? evt = null)
    {
        evt ??= EventEntityJson.ToEventJson(Some.MakeEvent(e => e.Properties = []));

        var output = new StringWriter();
        TextFormatters.Json(theme).Format(evt, output);
        return output.ToString();
    }
}
