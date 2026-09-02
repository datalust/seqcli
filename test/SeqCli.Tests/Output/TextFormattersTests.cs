#nullable enable
using System;
using System.IO;
using System.Text.Json.Nodes;
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
