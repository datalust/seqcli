#nullable enable
using System;
using System.IO;
using SeqCli.Output;
using SeqCli.Tests.Support;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Templates.Themes;
using Xunit;

namespace SeqCli.Tests.Output;

public class OutputFormatterTests
{
    const char Escape = '\x1b';
    static readonly DateTimeOffset FixedTimestamp = new DateTimeOffset(2024, 1, 1, 10, 0, 1, 250, TimeSpan.Zero);

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
    public void LogEventsAreFormattedWithTheDefaultTextTemplate()
    {
        var evt = SomeLogEvent(
            level: LogEventLevel.Warning,
            properties: new LogEventProperty("Name", new ScalarValue("world")));

        Assert.Equal(
            "[2024-01-01T10:00:01.2500000+00:00 WRN] Hello, world! {\"Name\":\"world\"}\n",
            RenderText(evt));
    }

    [Fact]
    public void ExceptionsAreIncludedInTextOutput()
    {
        var evt = SomeLogEvent(
            FixedTimestamp,
            LogEventLevel.Error,
            new Exception("Boom!"),
            new LogEventProperty("Name", new ScalarValue("world")));

        Assert.Equal(
            "[2024-01-01T10:00:01.2500000+00:00 ERR] Hello, world! {\"Name\":\"world\"}\nSystem.Exception: Boom!\n",
            RenderText(evt));
    }

    [Fact]
    public void SpanElapsedTimeIsComputedFromTheStartTimestampProperty()
    {
        // Events retrieved from the Seq API carry span start timestamps in ISO-8601 `@st` properties.
        var evt = SomeLogEvent(FixedTimestamp, properties:
        [
            new LogEventProperty("Name", new ScalarValue("world")),
            new LogEventProperty("@st", new ScalarValue("2024-01-01T10:00:00.0000000Z"))
        ]);

        Assert.Equal(
            "[2024-01-01T10:00:01.2500000+00:00 INF] Hello, world! (1250 ms) {\"Name\":\"world\",\"@st\":\"2024-01-01T10:00:00.0000000Z\"}\n",
            RenderText(evt));
    }

    [Fact]
    public void SpanElapsedTimeIsComputedFromTheSurrogateStartTimestampProperty()
    {
        // Ingested spans carry a surrogate `SpanStartTimestamp` property with a `DateTime` value.
        var evt = SomeLogEvent(FixedTimestamp, properties:
        [
            new LogEventProperty("Name", new ScalarValue("world")),
            new LogEventProperty("SpanStartTimestamp", new ScalarValue(
                FixedTimestamp.UtcDateTime.AddMilliseconds(-1.5)))
        ]);

        Assert.Equal(
            "[2024-01-01T10:00:01.2500000+00:00 INF] Hello, world! (1.5 ms) {\"Name\":\"world\",\"SpanStartTimestamp\":\"2024-01-01T10:00:01.2485000Z\"}\n",
            RenderText(evt));
    }

    [Fact]
    public void ACustomOutputTemplateReplacesTheDefault()
    {
        var evt = SomeLogEvent(properties: new LogEventProperty("Name", new ScalarValue("world")));

        Assert.Equal("INF Hello, world!\n", RenderText(evt, "{@l:u3} {@m}\n"));
    }

    static LogEvent SomeLogEvent(
        DateTimeOffset? timestamp = null,
        LogEventLevel level = LogEventLevel.Information,
        Exception? exception = null,
        params LogEventProperty[] properties)
    {
        return new LogEvent(
            timestamp ?? FixedTimestamp,
            level,
            exception,
            new MessageTemplateParser().Parse("Hello, {Name}!"),
            properties);
    }

    static string RenderText(LogEvent evt, string? outputTemplate = null)
    {
        var output = new StringWriter();
        OutputFormatter.Text(theme: null, outputTemplate).Format(evt, output);
        return output.ToString();
    }

    static string RenderJson(TemplateTheme? theme)
    {
        var evt = OutputFormat.ToSerilogEvent(Some.MakeEvent(e => e.Properties = []));

        var output = new StringWriter();
        OutputFormatter.Json(theme).Format(evt, output);
        return output.ToString();
    }
}
