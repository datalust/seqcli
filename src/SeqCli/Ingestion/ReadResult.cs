using Serilog.Events;

namespace SeqCli.Ingestion
{
    readonly struct ReadResult
    {
        public LogEvent? LogEvent { get; }
        public bool IsAtEnd { get; }

        public ReadResult(LogEvent? logEvent, bool isAtEnd)
        {
            LogEvent = logEvent;
            IsAtEnd = isAtEnd;
        }
    }
}