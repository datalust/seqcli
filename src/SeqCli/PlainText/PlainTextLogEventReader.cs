using System;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Ingestion;
using SeqCli.PlainText.Extraction;
using SeqCli.PlainText.Framing;
using SeqCli.PlainText.LogEvents;
using SeqCli.PlainText.Parsers;
using SeqCli.PlainText.Patterns;
using Serilog.Events;

namespace SeqCli.PlainText
{
    class PlainTextLogEventReader : ILogEventReader
    {
        static readonly TimeSpan TrailingLineArrivalDeadline = TimeSpan.FromMilliseconds(10);

        readonly NameValueExtractor _nameValueExtractor;
        readonly FrameReader _reader;

        public PlainTextLogEventReader(TextReader input, string extractionPattern)
        {
            if (extractionPattern == null) throw new ArgumentNullException(nameof(extractionPattern));
            _nameValueExtractor = ExtractionPatternInterpreter.CreateNameValueExtractor(ExtractionPatternParser.Parse(extractionPattern));
            
            _reader = new FrameReader(input, SpanEx.MatchedBy(_nameValueExtractor.StartMarker), TrailingLineArrivalDeadline);
        }

        public async Task<ReadResult> TryReadAsync()
        {
            var frame = await _reader.TryReadAsync();
            if (!frame.HasValue)
                return new ReadResult(null, frame.IsAtEnd);

            if (frame.IsOrphan)
                throw new InvalidDataException($"A line arrived late or could not be parsed: `{frame.Value.Trim()}`.");

            var (properties, remainder) = _nameValueExtractor.ExtractValues(frame.Value);
            var evt = LogEventBuilder.FromProperties(properties, remainder);
            return new ReadResult(evt, frame.IsAtEnd);
        }
    }
}
