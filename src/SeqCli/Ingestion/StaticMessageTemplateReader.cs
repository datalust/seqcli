using System;
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Util;
using Serilog.Events;
using Serilog.Parsing;

namespace SeqCli.Ingestion
{
    class StaticMessageTemplateReader : ILogEventReader
    {
        readonly ILogEventReader _inner;
        readonly MessageTemplate _messageTemplate;

        public StaticMessageTemplateReader(ILogEventReader inner, string messageTemplate)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _messageTemplate = new MessageTemplateParser().Parse(messageTemplate);
        }

        public async Task<ReadResult> TryReadAsync()
        {
            var result = await _inner.TryReadAsync();

            if (result.LogEvent == null)
                return result;

            var evt = new LogEvent(
                result.LogEvent.Timestamp,
                result.LogEvent.Level,
                result.LogEvent.Exception,
                _messageTemplate,
                result.LogEvent.Properties.Select(kv => LogEventPropertyFactory.SafeCreate(kv.Key, kv.Value)));

            return new ReadResult(evt, result.IsAtEnd);
        }
    }
}