using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Seq.Api.Model.Events;
using Seq.Api.Model.Shared;

namespace SeqCli.Tests.Support;

#nullable enable

static class Some
{
    static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public static JsonObject EventJson()
    {
        return new JsonObject
        {
            ["@t"] = DateTimeOffset.UtcNow.ToString("o"),
            ["@mt"] = "Test"
        };
    }

    public static string String()
    {
        return Guid.NewGuid().ToString("n");
    }

    public static string UriString()
    {
        return "https://example.com";
    }

    public static byte[] Bytes(int count)
    {
        var bytes = new byte[count];
        Rng.GetBytes(bytes);
        return bytes;
    }

    public static EventEntity MakeEvent(Action<EventEntity>? configure = null)
    {
        var evt = new EventEntity
        {
            Id = $"event-{String()}",
            Timestamp = "2024-01-01T00:00:00.0000000Z",
            RenderedMessage = "Hello",
            MessageTemplateTokens = [new MessageTemplateTokenPart { Text = "Hello" }],
            EventType = "$00000000",
        };
        configure?.Invoke(evt);
        return evt;
    }

    public static List<EventPropertyPart> MakeProperties(params (string Name, object? Value)[] items) =>
        items.Select(i => new EventPropertyPart(i.Name, i.Value)).ToList();
}
