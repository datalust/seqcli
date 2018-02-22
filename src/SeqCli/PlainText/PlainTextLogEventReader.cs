using System;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Ingestion;
using SeqCli.PlainText.Parsers;
using Serilog.Events;

namespace SeqCli.PlainText
{
    class PlainTextLogEventReader : ILogEventReader, IDisposable
    {
        static readonly TimeSpan TrailingLineArrivalDeadline = TimeSpan.FromMilliseconds(10);

        readonly Pattern _pattern;
        readonly FrameReader _reader;

        public PlainTextLogEventReader(TextReader input)
        {
            _pattern = PatternBuilder.DefaultPattern;
            _reader = new FrameReader(input, SpanEx.MatchedBy(_pattern.FrameStart), TrailingLineArrivalDeadline);
        }

        public async Task<LogEvent> TryReadAsync()
        {
            var frame = await _reader.TryReadAsync();
            if (!frame.HasValue)
                return null;

            if (frame.IsOrphan)
                throw new InvalidDataException($"A line arrived late or could not be parsed: `{frame.Value.Trim()}`.");

            var (properties, remainder) = _pattern.Match(frame.Value);
            return LogEventBuilder.FromProperties(properties, remainder);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}