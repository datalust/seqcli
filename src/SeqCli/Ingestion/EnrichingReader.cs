using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeqCli.Data;
using SeqCli.Syntax;

namespace SeqCli.Ingestion;

class EnrichingReader : IEventReader
{
    readonly IEventReader _inner;
    readonly IReadOnlyCollection<IEventEnricher> _enrichers;

    public EnrichingReader(
        IEventReader inner,
        IReadOnlyCollection<IEventEnricher> enrichers)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
    }

    public async Task<ReadResult> TryReadAsync()
    {
        var result = await _inner.TryReadAsync();

        if (result.Document != null)
        {
            foreach (var enricher in _enrichers)
                enricher.Enrich(result.Document);
        }

        return result;
    }
}
