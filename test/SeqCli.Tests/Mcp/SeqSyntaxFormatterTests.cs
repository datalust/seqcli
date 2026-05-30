#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Events;
using Seq.Api.Model.Shared;
using SeqCli.Mcp.Formatting;
using Xunit;

namespace SeqCli.Tests.Mcp;

public class SeqSyntaxFormatterTests
{
    [Theory]
    [InlineData("@Properties", "a", true, "a")]
    [InlineData("@Properties", "a b", true, "@Properties['a b']")]
    [InlineData("@Properties", "and", true, "@Properties.and")]
    [InlineData("@Resource", "a", false, "@Resource.a")]
    [InlineData("@Resource", "a b", false, "@Resource['a b']")]
    [InlineData("@Resource", "and", false, "@Resource.and")]
    public void IdentifiersAreIdiomaticallyFormatted(string prefix, string name, bool prefixIsOptional, string expected)
    {
        var actual = SeqSyntaxFormatter.MakeIdentifier(prefix, name, prefixIsOptional);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(BuiltInPropertyCases))]
    public void BuiltInPropertiesAreFormatted(EventEntity evt, string expectedLiteral)
    {
        Assert.Contains(expectedLiteral, Render(evt));
    }

    public static IEnumerable<object[]> BuiltInPropertyCases() =>
    [
        [MakeEvent(e => e.Id = "abc"), "@Id: 'abc'"],
        [MakeEvent(), "@Timestamp: DateTime('2024-01-01T00:00:00.0000000Z')"],
        [MakeEvent(e => e.Level = "Error"), "@Level: 'Error'"],
        [MakeEvent(e => e.RenderedMessage = "hello world"), "@Message: 'hello world'"],
        [
            MakeEvent(e => e.MessageTemplateTokens =
            [new MessageTemplateTokenPart { Text = "User " }, new MessageTemplateTokenPart { RawText = "{UserId}", PropertyName = "UserId" }]),
            "@MessageTemplate: 'User {UserId}'"
        ],
        [MakeEvent(e => e.EventType = "$0000000a"), "@EventType: 10"],
        [MakeEvent(e => e.Exception = "System.Exception: boom"), "@Exception: 'System.Exception: boom'"],
        [MakeEvent(e => e.Elapsed = TimeSpan.FromSeconds(13)), "@Elapsed: 13s"],
        [MakeEvent(e => e.TraceId = "abc123"), "@TraceId: 'abc123'"],
        [MakeEvent(e => e.SpanId = "def456"), "@SpanId: 'def456'"],
        [MakeEvent(e => e.SpanKind = "server"), "@SpanKind: 'server'"],
        [MakeEvent(e => e.Start = "2024-01-01T00:00:00.0000000Z"), "@Start: DateTime('2024-01-01T00:00:00.0000000Z')"],
        [MakeEvent(e => e.ParentId = "p1"), "@ParentId: 'p1'"],
        [MakeEvent(e => e.Properties = MakeProperties(("UserId", 42))), "@Properties: {UserId: 42}"],
        [MakeEvent(e => e.Scope = MakeProperties(("name", "myscope"))), "@Scope: {name: 'myscope'}"],
        [MakeEvent(e => e.Resource = MakeProperties(("host", "h"))), "@Resource: {host: 'h'}"],
        [MakeEvent(e => e.Definitions = MakeProperties(("d", 1))), "@Definitions: {d: 1}"]
    ];

    [Theory]
    [InlineData("@Exception")]
    [InlineData("@Elapsed")]
    [InlineData("@TraceId")]
    [InlineData("@SpanId")]
    [InlineData("@SpanKind")]
    [InlineData("@Start")]
    [InlineData("@ParentId")]
    [InlineData("@Properties")]
    [InlineData("@Scope")]
    [InlineData("@Resource")]
    [InlineData("@Definitions")]
    public void OptionalPropertiesAreOmittedWhenAbsent(string token)
    {
        Assert.DoesNotContain(token, Render(MakeEvent()));
    }

    [Fact]
    public void EmptyPropertyCollectionIsOmitted()
    {
        Assert.DoesNotContain("@Properties", Render(MakeEvent(e => e.Properties = [])));
    }

    [Fact]
    public void EventFormatIsAnObjectLiteral()
    {
        Assert.Equal(
            "{@Id: 'event-1', @Timestamp: DateTime('2024-01-01T00:00:00.0000000Z'), " +
            "@Level: 'Information', @Message: 'Hello', @MessageTemplate: 'Hello', @EventType: 0}",
            Render(MakeEvent()));
    }

    [Theory]
    [MemberData(nameof(BasicPropertyFormattingCases))]
    public void BasicPropertiesAreFormatted(EventEntity evt, string expectedLiteral)
    {
        Assert.Contains(expectedLiteral, Render(evt));
    }

    public static IEnumerable<object[]> BasicPropertyFormattingCases() =>
    [
        [MakeEvent(e => e.RenderedMessage = "it's"), "@Message: 'it''s'"],
        [MakeEvent(e => e.Level = null), "@Level: 'Information'"],
        [MakeEvent(e => e.Timestamp = "2024-01-01T12:00:00+02:00"), "@Timestamp: DateTime('2024-01-01T10:00:00.0000000Z')"
        ],
        [MakeEvent(e => e.EventType = "$c0ffee00"), "@EventType: 3237998080"],
        [MakeEvent(e => e.EventType = "$00000000"), "@EventType: 0"],
        [MakeEvent(e => e.MessageTemplateTokens = [new MessageTemplateTokenPart { PropertyName = "X" }]), "@MessageTemplate: '{X}'"
        ],
        [MakeEvent(e => e.Properties = MakeProperties(("request id", 5))), "@Properties: {'request id': 5}"],
        [MakeEvent(e => e.Properties = MakeProperties(("n", 42))), "@Properties: {n: 42}"],
        [MakeEvent(e => e.Properties = MakeProperties(("s", "x"))), "@Properties: {s: 'x'}"],
        [MakeEvent(e => e.Properties = MakeProperties(("a", 1), ("b", true))), "@Properties: {a: 1, b: true}"],
        [MakeEvent(e => e.Properties = MakeProperties(("b", true))), "@Properties: {b: true}"],
        [MakeEvent(e => e.Properties = MakeProperties(("z", null))), "@Properties: {z: null}"]
    ];

    [Theory]
    [MemberData(nameof(NestedPropertyCases))]
    public void NestedPropertiesAreFormatted(EventEntity evt, string expectedLiteral)
    {
        Assert.Contains(expectedLiteral, Render(evt));
    }

    public static IEnumerable<object[]> NestedPropertyCases() =>
    [
        [
            MakeEvent(e => e.Resource = MakeProperties(("service", new JObject { ["name"] = "web" }))),
            "@Resource: {service: {name: 'web'}}"
        ],
        [
            MakeEvent(e => e.Resource = MakeProperties(("service", new JObject { ["name"] = "web", ["version"] = "1.0" }))),
            "@Resource: {service: {name: 'web', version: '1.0'}}"
        ],
        [
            MakeEvent(e => e.Resource = MakeProperties(("service", new JObject { ["namespace"] = new JObject { ["name"] = "web" } }))),
            "@Resource: {service: {namespace: {name: 'web'}}}"
        ],
        [
            MakeEvent(e => e.Properties = MakeProperties(("http", new JObject { ["request"] = new JObject { ["method"] = "GET" } }))),
            "@Properties: {http: {request: {method: 'GET'}}}"
        ],
        [
            MakeEvent(e => e.Scope = MakeProperties(("db", new JObject { ["system"] = "postgres" }))),
            "@Scope: {db: {system: 'postgres'}}"
        ],
        [
            MakeEvent(e => e.Properties = MakeProperties(("http", new JObject { ["content-type"] = "json" }))),
            "@Properties: {http: {'content-type': 'json'}}"
        ],
        [
            MakeEvent(e => e.Properties = MakeProperties(("tags", new JArray("a", "b")))),
            "@Properties: {tags: ['a', 'b']}"
        ]
    ];
    
    static EventEntity MakeEvent(Action<EventEntity>? configure = null)
    {
        var evt = new EventEntity
        {
            Id = "event-1",
            Timestamp = "2024-01-01T00:00:00.0000000Z",
            RenderedMessage = "Hello",
            MessageTemplateTokens = [new MessageTemplateTokenPart { Text = "Hello" }],
            EventType = "$00000000",
        };
        configure?.Invoke(evt);
        return evt;
    }

    static List<EventPropertyPart> MakeProperties(params (string Name, object? Value)[] items) =>
        items.Select(i => new EventPropertyPart(i.Name, i.Value)).ToList();

    static string Render(EventEntity evt)
    {
        var output = new StringWriter();
        SeqSyntaxFormatter.WriteEvent(output, evt);
        return output.ToString();
    }
}
