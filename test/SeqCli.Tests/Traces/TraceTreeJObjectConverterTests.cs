#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using SeqCli.Traces;
using Xunit;

namespace SeqCli.Tests.Traces;

public class TraceTreeJObjectConverterTests
{
    const string TraceId = "46a2b061ad71d2e7f93e5332012b8477";

    static readonly DateTimeOffset T0 = new(2026, 7, 31, 10, 20, 0, TimeSpan.Zero);

    static TraceTreeElement Span(string spanId, string? parentId, double startMs = 0, double elapsedMs = 1,
        string? message = null, string? level = null, string? exception = null,
        IReadOnlyList<object?>? columns = null) =>
        new($"event-span-{spanId}", T0.AddMilliseconds(startMs + elapsedMs), level,
            message ?? $"span {spanId}", new JsonObject(),
            exception, spanId, parentId, T0.AddMilliseconds(startMs), TimeSpan.FromMilliseconds(elapsedMs),
            columns ?? []);

    static TraceTreeElement Log(string? spanId, double timestampMs, string message = "log", string? level = null,
        string? exception = null, IReadOnlyList<object?>? columns = null) =>
        new($"event-log-{timestampMs}-{message}", T0.AddMilliseconds(timestampMs), level,
            message, new JsonObject(), exception,
            spanId, null, null, null, columns ?? []);

    static JObject ToJson(params TraceTreeElement[] events) => ToJson([], events);

    static JObject ToJson(IReadOnlyList<string> columns, params TraceTreeElement[] events) =>
        TraceTreeJObjectConverter.FromRoots(TraceId, TraceTreeBuilder.Build(events), complete: true, includeTypeMarker: true, columns);

    [Fact]
    public void TheDocumentCarriesTraceMetadataAndTheRootSpan()
    {
        var document = ToJson(Span("a", null, startMs: 0, elapsedMs: 1.5, message: "GET /orders"));

        Assert.Equal(TraceId, (string?)document["traceId"]);
        Assert.True((bool?)document["complete"]);
        Assert.Null(document["orphans"]);

        var root = (JObject)document["root"]!;
        Assert.Equal("span", (string?)root["type"]);
        Assert.Equal("a", (string?)root["spanId"]);
        Assert.Equal("2026-07-31T10:20:00.0000000Z", (string?)root["start"]);
        Assert.Equal(1.5, (double?)root["elapsedMs"]);
        Assert.Equal("GET /orders", (string?)root["message"]);
        Assert.Null(root["children"]);
        Assert.Null(root["parentSpanId"]);
        Assert.Null(root["level"]);
        Assert.Null(root["timestamp"]);
        Assert.Null(root["exception"]);
        Assert.Null(root["columns"]);
    }

    [Fact]
    public void ChildSpansAndLogsInterleaveChronologicallyInChildren()
    {
        var document = ToJson(
            Span("a", null, startMs: 0, elapsedMs: 10),
            Log("a", 1, "first"),
            Span("b", "a", startMs: 2, elapsedMs: 5),
            Log("b", 3, "nested"),
            Log("a", 7, "last"));

        var children = (JArray)document["root"]!["children"]!;
        Assert.Equal(
            [("log", "first"), ("span", "span b"), ("log", "last")],
            children.Select(c => ((string?)c["type"], (string?)c["message"])).ToArray());

        var log = (JObject)children[0];
        Assert.Equal("a", (string?)log["spanId"]);
        Assert.Equal("2026-07-31T10:20:00.0010000Z", (string?)log["timestamp"]);
        Assert.Null(log["start"]);
        Assert.Null(log["elapsedMs"]);
        Assert.Null(log["children"]);

        var nested = Assert.Single((JArray)children[1]["children"]!);
        Assert.Equal("nested", (string?)nested["message"]);
        Assert.Equal("a", (string?)children[1]["parentSpanId"]);
    }

    [Fact]
    public void OrphanSpansCarryTheirUnresolvedParentSpanIdAndSubtrees()
    {
        var document = ToJson(
            Span("a", null),
            Span("b", "missing", startMs: 1),
            Span("c", "b", startMs: 2));

        Assert.Equal("a", (string?)document["root"]!["spanId"]);

        var orphan = (JObject)Assert.Single((JArray)document["orphans"]!);
        Assert.Equal("b", (string?)orphan["spanId"]);
        Assert.Equal("missing", (string?)orphan["parentSpanId"]);

        var child = Assert.Single((JArray)orphan["children"]!);
        Assert.Equal("c", (string?)child["spanId"]);
    }

    [Fact]
    public void AdditionalParentlessSpansBecomeOrphans()
    {
        var document = ToJson(
            Span("b", null, startMs: 5),
            Span("a", null, startMs: 0));

        Assert.Equal("a", (string?)document["root"]!["spanId"]);

        var orphan = (JObject)Assert.Single((JArray)document["orphans"]!);
        Assert.Equal("b", (string?)orphan["spanId"]);
        Assert.Null(orphan["parentSpanId"]);
    }

    [Fact]
    public void TheRootIsNullWhenNoRootSpanWasCaptured()
    {
        var document = ToJson(Span("b", "missing"));

        Assert.Equal(JTokenType.Null, document["root"]!.Type);
        Assert.Single((JArray)document["orphans"]!);
    }

    [Fact]
    public void LogsWithNoCapturedEnclosingSpanBecomeOrphans()
    {
        var document = ToJson(
            Span("a", null),
            Log("uncaptured", 1, "first"),
            Span("b", "missing", startMs: 2),
            Log(null, 3, "second"));

        var orphans = (JArray)document["orphans"]!;
        Assert.Equal(
            [("log", "first"), ("span", "span b"), ("log", "second")],
            orphans.Select(o => ((string?)o["type"], (string?)o["message"])).ToArray());
        Assert.Equal("uncaptured", (string?)orphans[0]["spanId"]);
        Assert.Null(orphans[2]["spanId"]);
    }
    
    [Fact]
    public void TemplateHolesAreFilledFromMessageProperties()
    {
        var evt = new TraceTreeElement("event-1", T0.AddMilliseconds(1.5), null,
            "GET {Route} as {User}",
            new JsonObject { ["Route"] = "/orders" },
            null, "a", null, T0, TimeSpan.FromMilliseconds(1.5), []);

        var document = ToJson(evt);

        Assert.Equal("GET /orders as {User}", (string?)document["root"]!["message"]);
    }

    [Fact]
    public void ExceptionsAreIncludedWhenPresent()
    {
        var document = ToJson(
            Span("a", null),
            Log("a", 1, "Failed", level: "err", exception: "System.Exception: Boom!"));

        var log = Assert.Single((JArray)document["root"]!["children"]!);
        Assert.Equal("System.Exception: Boom!", (string?)log["exception"]);
    }

    [Fact]
    public void ColumnValuesAreKeyedByExpression()
    {
        var document = ToJson(
            ["@Resource.service.name", "Missing", "OrderId"],
            Span("a", null, columns: ["frontend", null, 42L]),
            Span("b", "a", startMs: 1, columns: [null, null, null]));

        var columns = (JObject)document["root"]!["columns"]!;
        Assert.Equal("frontend", (string?)columns["@Resource.service.name"]);
        Assert.Equal(42L, (long?)columns["OrderId"]);
        Assert.Null(columns["Missing"]);

        var child = Assert.Single((JArray)document["root"]!["children"]!);
        Assert.Null(child["columns"]);
    }

    [Fact]
    public void IncompleteTracesAreFlagged()
    {
        var document = TraceTreeJObjectConverter.FromRoots(
            TraceId, TraceTreeBuilder.Build([Span("a", null)]), complete: false, includeTypeMarker: true, []);

        Assert.False((bool?)document["complete"]);
    }

    [Fact]
    public void TypeMarkersAreOnlyIncludedWhenRequested()
    {
        var roots = TraceTreeBuilder.Build([
            Span("a", null),
            Span("b", "a", startMs: 1),
            Span("c", "missing", startMs: 2)
        ]);

        var document = TraceTreeJObjectConverter.FromRoots(TraceId, roots, complete: true, includeTypeMarker: false, []);

        var root = (JObject)document["root"]!;
        Assert.Null(root["type"]);
        var child = Assert.Single((JArray)root["children"]!);
        Assert.Null(child["type"]);
        var orphan = (JObject)Assert.Single((JArray)document["orphans"]!);
        Assert.Null(orphan["type"]);

        var subtree = TraceTreeJObjectConverter.FromSubtree(
            TraceId, TraceTreeBuilder.FindSpan(roots, "b")!, complete: true, includeTypeMarker: false, []);
        Assert.Null(subtree["root"]!["type"]);
    }

    [Fact]
    public void SubtreeDocumentsOmitOrphans()
    {
        var roots = TraceTreeBuilder.Build([
            Span("a", null),
            Span("b", "a", startMs: 1),
            Log("b", 2, "nested"),
            Log("uncaptured", 3, "orphan")
        ]);

        var subtreeRoot = TraceTreeBuilder.FindSpan(roots, "b");
        Assert.NotNull(subtreeRoot);

        var document = TraceTreeJObjectConverter.FromSubtree(TraceId, subtreeRoot, complete: true, includeTypeMarker: true, []);

        Assert.Equal(TraceId, (string?)document["traceId"]);
        Assert.True((bool?)document["complete"]);
        Assert.Null(document["orphans"]);

        var root = (JObject)document["root"]!;
        Assert.Equal("b", (string?)root["spanId"]);
        Assert.Equal("a", (string?)root["parentSpanId"]);
        var child = Assert.Single((JArray)root["children"]!);
        Assert.Equal("nested", (string?)child["message"]);
    }
}
