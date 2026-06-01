using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Seq.Api.Model.Events;
using Seq.Api.Model.Shared;
using Serilog.Events;
using Serilog.Parsing;

namespace SeqCli.Tests.Support;

#nullable enable

static class Some
{
    static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public static LogEvent LogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow, 
            LogEventLevel.Information,
            null,
            new MessageTemplateParser().Parse("Test"),
            Enumerable.Empty<LogEventProperty>());
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