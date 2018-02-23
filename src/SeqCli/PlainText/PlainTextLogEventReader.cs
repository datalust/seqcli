using System;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Ingestion;
using SeqCli.PlainText.Extraction;
using SeqCli.PlainText.Parsers;
using SeqCli.PlainText.Patterns;
using Serilog.Events;

namespace SeqCli.PlainText
{
    class PlainTextLogEventReader : ILogEventReader, IDisposable
    {
        static readonly TimeSpan TrailingLineArrivalDeadline = TimeSpan.FromMilliseconds(10);

        readonly NameValueExtractor _nameValueExtractor;
        readonly FrameReader _reader;

        public PlainTextLogEventReader(TextReader input, string extractionPattern)
        {
            _nameValueExtractor = string.IsNullOrEmpty(extractionPattern) ?
                PatternCompiler.MultilineMessageExtractor :
                PatternCompiler.Compile(ExtractionPatternParser.Parse(extractionPattern));
            
            _reader = new FrameReader(input, SpanEx.MatchedBy(_nameValueExtractor.StartMarker), TrailingLineArrivalDeadline);
        }

        public async Task<LogEvent> TryReadAsync()
        {
            var frame = await _reader.TryReadAsync();
            if (!frame.HasValue)
                return null;

            if (frame.IsOrphan)
                throw new InvalidDataException($"A line arrived late or could not be parsed: `{frame.Value.Trim()}`.");

            var (properties, remainder) = _nameValueExtractor.ExtractValues(frame.Value);
            return LogEventBuilder.FromProperties(properties, remainder);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}