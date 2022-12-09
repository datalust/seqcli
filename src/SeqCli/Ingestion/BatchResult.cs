using Serilog.Events;

namespace SeqCli.Ingestion;

struct BatchResult
{
    public LogEvent[] LogEvents { get; }
    public bool IsLast { get; }

    public BatchResult(LogEvent[] logEvents, bool isLast)
    {
        LogEvents = logEvents;
        IsLast = isLast;
    }
}