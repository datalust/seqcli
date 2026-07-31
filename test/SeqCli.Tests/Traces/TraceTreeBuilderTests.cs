#nullable enable
using System;
using System.Linq;
using SeqCli.Traces;
using Xunit;

namespace SeqCli.Tests.Traces;

public class TraceTreeBuilderTests
{
    static readonly DateTimeOffset T0 = new(2026, 7, 31, 10, 20, 0, TimeSpan.Zero);

    static TraceEvent Span(string spanId, string? parentId, double startMs = 0, double elapsedMs = 1) =>
        new($"event-span-{spanId}", T0.AddMilliseconds(startMs + elapsedMs), null, $"span {spanId}", null,
            spanId, parentId, T0.AddMilliseconds(startMs), TimeSpan.FromMilliseconds(elapsedMs), []);

    static TraceEvent Log(string? spanId, double timestampMs, string message = "log") =>
        new($"event-log-{timestampMs}-{message}", T0.AddMilliseconds(timestampMs), null, message, null,
            spanId, null, null, null, []);

    [Fact]
    public void ASingleSpanBecomesTheRoot()
    {
        var roots = TraceTreeBuilder.Build([Span("a", null)]);

        var root = Assert.Single(roots);
        Assert.Equal("a", root.Event.SpanId);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void ChildSpansNestUnderTheirParents()
    {
        var roots = TraceTreeBuilder.Build([
            Span("c", "b", startMs: 2),
            Span("a", null, startMs: 0, elapsedMs: 10),
            Span("b", "a", startMs: 1, elapsedMs: 5)
        ]);

        var root = Assert.Single(roots);
        var child = Assert.Single(root.Children);
        var grandchild = Assert.Single(child.Children);
        Assert.Equal("a", root.Event.SpanId);
        Assert.Equal("b", child.Event.SpanId);
        Assert.Equal("c", grandchild.Event.SpanId);
    }

    [Fact]
    public void LogsAttachToTheirEnclosingSpan()
    {
        var roots = TraceTreeBuilder.Build([
            Span("a", null),
            Log("a", 0.5)
        ]);

        var root = Assert.Single(roots);
        var log = Assert.Single(root.Children);
        Assert.False(log.Event.IsSpan);
    }

    [Fact]
    public void SiblingSpansAndLogsInterleaveChronologically()
    {
        var roots = TraceTreeBuilder.Build([
            Span("a", null, startMs: 0, elapsedMs: 10),
            Span("b", "a", startMs: 3),
            Log("a", 1, "first"),
            Span("c", "a", startMs: 2),
            Log("a", 4, "last")
        ]);

        var root = Assert.Single(roots);
        Assert.Equal(
            ["first", "span c", "span b", "last"],
            root.Children.Select(c => c.Event.Message).ToArray());
    }

    [Fact]
    public void SpansWithMissingParentsBecomeRoots()
    {
        var roots = TraceTreeBuilder.Build([
            Span("b", "missing", startMs: 5),
            Span("a", null, startMs: 0)
        ]);

        Assert.Equal(2, roots.Count);
        Assert.Equal("a", roots[0].Event.SpanId);
        Assert.Equal("b", roots[1].Event.SpanId);
    }

    [Fact]
    public void OrphanLogsAttachToTheFirstRootSpan()
    {
        var roots = TraceTreeBuilder.Build([
            Span("b", null, startMs: 5),
            Span("a", null, startMs: 0),
            Log("uncaptured", 1),
            Log(null, 2)
        ]);

        Assert.Equal(2, roots.Count);
        Assert.Equal("a", roots[0].Event.SpanId);
        Assert.Equal(2, roots[0].Children.Count);
        Assert.Empty(roots[1].Children);
    }

    [Fact]
    public void OrphanLogsWithNoRootSpanRemainAtRootLevel()
    {
        var roots = TraceTreeBuilder.Build([
            Log("uncaptured", 1, "first"),
            Log(null, 2, "second")
        ]);

        Assert.Equal(2, roots.Count);
        Assert.Equal(["first", "second"], roots.Select(r => r.Event.Message).ToArray());
    }

    [Fact]
    public void DuplicateSpanIdsAreTolerated()
    {
        var roots = TraceTreeBuilder.Build([
            Span("a", null),
            Span("a", null, startMs: 1),
            Span("b", "a", startMs: 2)
        ]);

        Assert.Equal(2, roots.Count);
        var child = Assert.Single(roots[0].Children);
        Assert.Equal("b", child.Event.SpanId);
    }

    [Fact]
    public void ASelfParentedSpanBecomesARoot()
    {
        var roots = TraceTreeBuilder.Build([Span("a", "a")]);

        var root = Assert.Single(roots);
        Assert.Empty(root.Children);
    }

    [Fact]
    public void ParentCyclesAreBrokenWithoutLosingEvents()
    {
        var roots = TraceTreeBuilder.Build([
            Span("a", "b"),
            Span("b", "a", startMs: 1)
        ]);

        var root = Assert.Single(roots);
        var child = Assert.Single(root.Children);
        Assert.Empty(child.Children);
    }
}
