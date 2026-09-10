using System;
using System.Threading.Tasks;

namespace SeqCli.Ingestion;

class StaticMessageTemplateReader : IEventReader
{
    readonly IEventReader _inner;
    readonly string _messageTemplate;

    public StaticMessageTemplateReader(IEventReader inner, string messageTemplate)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _messageTemplate = messageTemplate ?? throw new ArgumentNullException(nameof(messageTemplate));
    }

    public async Task<ReadResult> TryReadAsync()
    {
        var result = await _inner.TryReadAsync();

        if (result.Document != null)
        {
            result.Document.Remove("@m");
            result.Document["@mt"] = _messageTemplate;
        }

        return result;
    }
}
