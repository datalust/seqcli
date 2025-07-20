using System.Collections.Generic;

namespace SeqCli.PlainText;

static class ReifiedProperties
{
    public const string
        Message = "@m",
        Timestamp = "@t",
        Level = "@l",
        Exception = "@x",
        StartTimestamp = "@st",
        SpanId = "@sp",
        TraceId = "@tr";
        
    static readonly HashSet<string> All = [Message, Timestamp, Level, Exception, StartTimestamp, SpanId, TraceId];

    public static bool IsReifiedProperty(string name)
    {
        return All.Contains(name);
    }
}