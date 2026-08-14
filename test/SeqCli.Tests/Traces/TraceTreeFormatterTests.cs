#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SeqCli.Output;
using SeqCli.Traces;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace SeqCli.Tests.Traces;

public class TraceTreeFormatterTests
{
    static readonly DateTimeOffset T0 = new(2026, 7, 31, 10, 20, 0, TimeSpan.Zero);

    static TraceTreeElement Span(string spanId, string? parentId, double startMs = 0, double elapsedMs = 1,
        string? message = null, IReadOnlyList<object?>? columns = null) =>
        new($"event-span-{spanId}", T0.AddMilliseconds(startMs + elapsedMs), null,
            new MessageTemplate([new TextToken(message ?? $"span {spanId}")]), [],
            null, spanId, parentId, T0.AddMilliseconds(startMs), TimeSpan.FromMilliseconds(elapsedMs),
            columns ?? []);

    static TraceTreeElement Log(string? spanId, double timestampMs, string message = "log", string? level = null,
        string? exception = null) =>
        new($"event-log-{timestampMs}-{message}", T0.AddMilliseconds(timestampMs), level,
            new MessageTemplate([new TextToken(message)]), [], exception,
            spanId, null, null, null, []);

    static string Render(params TraceTreeElement[] events)
    {
        var output = new StringWriter();
        var formatter = TextFormatters.Plain(theme: null,
            TraceShowFormat.OutputTemplate(events.Max(e => e.Columns.Count)));
        foreach (var logEvent in TraceTreeFormatter.ToLogEvents(TraceTreeBuilder.Build(events)))
            formatter.Format(logEvent, output);
        return output.ToString();
    }

    static string At(double offsetMs = 0) => T0.AddMilliseconds(offsetMs).ToLocalTime().ToString("o");

    [Fact]
    public void SpanLinesShowStartTimeAndElapsed()
    {
        Assert.Equal(
            $"[{At()} INF] span a (1.5 ms){Environment.NewLine}",
            Render(Span("a", null, startMs: 0, elapsedMs: 1.5)));
    }

    [Fact]
    public void LogLinesShowLevelAndNoElapsed()
    {
        Assert.Equal(
            $"[{At(1)} WRN] ┊  Watch out!{Environment.NewLine}",
            Render(
                Span("a", null, message: "root"),
                Log("a", 1, "Watch out!", level: "warn")).Split(Environment.NewLine)[1] + Environment.NewLine);
    }

    [Fact]
    public void TheTraceTreeIsDrawnWithConnectingGlyphs()
    {
        var actual = Render(
            Span("root", null, startMs: 0, elapsedMs: 245, message: "GET /orders"),
            Log("root", 1, "Request authenticated"),
            Span("q", "root", startMs: 2, elapsedMs: 180, message: "Query orders"),
            Span("sel", "q", startMs: 3, elapsedMs: 170, message: "SELECT * FROM orders"),
            Log("sel", 4, "42 rows retrieved"),
            Span("mat", "q", startMs: 175, elapsedMs: 8, message: "Materialize results"),
            Log("root", 181, "Cache miss", level: "warn"),
            Span("render", "root", startMs: 200, elapsedMs: 30, message: "Render response"),
            Log("uncaptured", 210, "Orphan log"));

        var expected = string.Join(Environment.NewLine,
        [
            $"[{At()} INF] GET /orders (245 ms)",
            $"[{At(1)} INF] ┊  Request authenticated",
            $"[{At(2)} INF] ├─ Query orders (180 ms)",
            $"[{At(3)} INF] │  ├─ SELECT * FROM orders (170 ms)",
            $"[{At(4)} INF] │  │  ┊  42 rows retrieved",
            $"[{At(175)} INF] │  └─ Materialize results (8 ms)",
            $"[{At(181)} WRN] ┊  Cache miss",
            $"[{At(200)} INF] └─ Render response (30 ms)",
            $"[{At(210)} INF] ┊  Orphan log",
            ""
        ]);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnsPrecedeTheMessageInOrder()
    {
        var actual = Render(Span("a", null, elapsedMs: 2, message: "GET /orders", columns: ["frontend", 42L]));

        Assert.Equal(
            $"[{At()} INF] frontend 42 GET /orders (2 ms){Environment.NewLine}",
            actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MissingAndEmptyColumnValuesLeaveNoRedundantSpace(object? first)
    {
        var actual = Render(Span("a", null, elapsedMs: 2, message: "GET /orders", columns: [first, 42L]));

        Assert.Equal(
            $"[{At()} INF] 42 GET /orders (2 ms){Environment.NewLine}",
            actual);
    }

    [Fact]
    public void TemplateHolesAreFilledFromMessageProperties()
    {
        var evt = new TraceTreeElement("event-1", T0.AddMilliseconds(1.5), null,
            new MessageTemplate([
                new TextToken("GET "),
                new PropertyToken("Route", "{Route}"),
                new TextToken(" as "),
                new PropertyToken("User", "{User}")]),
            [new LogEventProperty("Route", new ScalarValue("/orders"))],
            null, "a", null, T0, TimeSpan.FromMilliseconds(1.5), []);

        Assert.Equal(
            $"[{At()} INF] GET /orders as {{User}} (1.5 ms){Environment.NewLine}",
            Render(evt));
    }

    [Fact]
    public void ExceptionsAreIncluded()
    {
        var actual = Render(
            Span("a", null, message: "root"),
            Log("a", 1, "Failed", level: "err", exception: "System.Exception: Boom!"));

        Assert.EndsWith(
            $"[{At(1)} ERR] ┊  Failed{Environment.NewLine}System.Exception: Boom!{Environment.NewLine}",
            actual);
    }

    [Fact]
    public void RootLevelLogsAreMarked()
    {
        Assert.StartsWith($"[{At(1)} INF] ┊  only a log", Render(Log("a", 1, "only a log")));
    }
}
