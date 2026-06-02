#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Events;
using SeqCli.Tests.Support;
using Xunit;
using NativeFormatter = SeqCli.Output.NativeFormatter;

namespace SeqCli.Tests.Output;

public class NativeFormatterTests
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
        var actual = NativeFormatter.MakeIdentifier(prefix, name, prefixIsOptional);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(EventPropertyCases))]
    public void EventPropertiesAreFormatted(EventEntity evt, string expectedLiteral)
    {
        Assert.Contains(expectedLiteral, Render(evt));
    }

    public static IEnumerable<object[]> EventPropertyCases() =>
    [
        [Some.MakeEvent(e => e.Id = "abc"), "@Id: 'abc'"],
        [Some.MakeEvent(e => e.Timestamp = "2024-01-01T00:00:00.0000000Z"), "@Timestamp: DateTime('2024-01-01T00:00:00.0000000Z')"],
        [Some.MakeEvent(e => e.Level = "Error"), "@Level: 'Error'"],
        [Some.MakeEvent(e => e.RenderedMessage = "hello world"), "@Message: 'hello world'"],
        [Some.MakeEvent(e => e.RenderedMessage = "it's"), "@Message: 'it''s'"],
        [
            Some.MakeEvent(e => e.MessageTemplateTokens =
            [new MessageTemplateTokenPart { Text = "User " }, new MessageTemplateTokenPart { RawText = "{UserId}", PropertyName = "UserId" }]),
            "@MessageTemplate: 'User {UserId}'"
        ],
        [Some.MakeEvent(e => e.EventType = "$0000000a"), "@EventType: 10"],
        [Some.MakeEvent(e => e.EventType = "$c0ffee00"), "@EventType: 3237998080"],
        [Some.MakeEvent(e => e.EventType = "$00000000"), "@EventType: 0"],
        [Some.MakeEvent(e => e.Exception = "System.Exception: boom"), "@Exception: 'System.Exception: boom'"],
        [Some.MakeEvent(e => e.Elapsed = TimeSpan.FromSeconds(13)), "@Elapsed: 13s"],
        [Some.MakeEvent(e => e.TraceId = "abc123"), "@TraceId: 'abc123'"],
        [Some.MakeEvent(e => e.SpanId = "def456"), "@SpanId: 'def456'"],
        [Some.MakeEvent(e => e.SpanKind = "server"), "@SpanKind: 'server'"],
        [Some.MakeEvent(e => e.Start = "2024-01-01T00:00:00.0000000Z"), "@Start: DateTime('2024-01-01T00:00:00.0000000Z')"],
        [Some.MakeEvent(e => e.ParentId = "p1"), "@ParentId: 'p1'"],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("UserId", 42))), "@Properties: {UserId: 42}"],
        [Some.MakeEvent(e => e.Scope = Some.MakeProperties(("name", "myscope"))), "@Scope: {name: 'myscope'}"],
        [Some.MakeEvent(e => e.Resource = Some.MakeProperties(("host", "h"))), "@Resource: {host: 'h'}"],
        [Some.MakeEvent(e => e.Definitions = Some.MakeProperties(("d", 1))), "@Definitions: {d: 1}"],
        [Some.MakeEvent(e => e.Level = null), "@Level: 'Information'"],
        [Some.MakeEvent(e => e.Timestamp = "2024-01-01T12:00:00+02:00"), "@Timestamp: DateTime('2024-01-01T10:00:00.0000000Z')"
        ],
        [Some.MakeEvent(e => e.MessageTemplateTokens = [new MessageTemplateTokenPart { PropertyName = "X" }]), "@MessageTemplate: '{X}'"
        ],
        [Some.MakeEvent(e => e.MessageTemplateTokens = [new MessageTemplateTokenPart { Text = "{bracketed}" }]), "@MessageTemplate: '{{bracketed}}'"
        ],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("request id", 5))), "@Properties: {'request id': 5}"],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("n", 42))), "@Properties: {n: 42}"],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("s", "x"))), "@Properties: {s: 'x'}"],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("a", 1), ("b", true))), "@Properties: {a: 1, b: true}"],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("b", true))), "@Properties: {b: true}"],
        [Some.MakeEvent(e => e.Properties = Some.MakeProperties(("z", null))), "@Properties: {z: null}"],
        [
            Some.MakeEvent(e => e.Resource = Some.MakeProperties(("service", new JObject { ["name"] = "web" }))),
            "@Resource: {service: {name: 'web'}}"
        ],
        [
            Some.MakeEvent(e => e.Resource = Some.MakeProperties(("service", new JObject { ["name"] = "web", ["version"] = "1.0" }))),
            "@Resource: {service: {name: 'web', version: '1.0'}}"
        ],
        [
            Some.MakeEvent(e => e.Resource = Some.MakeProperties(("service", new JObject { ["namespace"] = new JObject { ["name"] = "web" } }))),
            "@Resource: {service: {namespace: {name: 'web'}}}"
        ],
        [
            Some.MakeEvent(e => e.Properties = Some.MakeProperties(("http", new JObject { ["request"] = new JObject { ["method"] = "GET" } }))),
            "@Properties: {http: {request: {method: 'GET'}}}"
        ],
        [
            Some.MakeEvent(e => e.Scope = Some.MakeProperties(("db", new JObject { ["system"] = "postgres" }))),
            "@Scope: {db: {system: 'postgres'}}"
        ],
        [
            Some.MakeEvent(e => e.Properties = Some.MakeProperties(("http", new JObject { ["content-type"] = "json" }))),
            "@Properties: {http: {'content-type': 'json'}}"
        ],
        [
            Some.MakeEvent(e => e.Properties = Some.MakeProperties(("tags", new JArray("a", "b")))),
            "@Properties: {tags: ['a', 'b']}"
        ]
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
        Assert.DoesNotContain(token, Render(Some.MakeEvent()));
    }

    [Fact]
    public void EmptyPropertyCollectionIsOmitted()
    {
        Assert.DoesNotContain("@Properties", Render(Some.MakeEvent(e => e.Properties = [])));
    }

    [Fact]
    public void EventFormatIsAnObjectLiteral()
    {
        Assert.Equal(
            "{@Id: 'event-1', @Timestamp: DateTime('2024-01-02T00:00:00.0000002Z'), " +
            "@Level: 'Information', @Message: 'Hello!', @MessageTemplate: 'Hello!', @EventType: 1}",
            Render(Some.MakeEvent(e =>
            {
                e.Id = "event-1";
                e.Timestamp = "2024-01-02T00:00:00.0000002Z";
                e.RenderedMessage = "Hello!";
                e.MessageTemplateTokens = [new MessageTemplateTokenPart { Text = "Hello!" }];
                e.EventType = "$00000001";
            })));
    }
    
    static string Render(EventEntity evt)
    {
        var output = new StringWriter();
        NativeFormatter.WriteEvent(output, evt);
        return output.ToString();
    }
}
