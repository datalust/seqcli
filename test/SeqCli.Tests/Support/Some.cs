using System;
using System.Linq;
using System.Security.Cryptography;
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
        return "http://example.com";
    }

    public static byte[] Bytes(int count)
    {
        var bytes = new byte[count];
        Rng.GetBytes(bytes);
        return bytes;
    }

    public static string ApiKey()
    {
        return string.Join("", Bytes(8).Select(v => v.ToString("x2")).ToArray());
    }
}