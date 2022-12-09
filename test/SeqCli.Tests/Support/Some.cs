using System;
using System.Linq;
using Serilog.Events;
using Serilog.Parsing;

namespace SeqCli.Tests.Support;

static class Some
{
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
}