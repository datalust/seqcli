using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog.Core;

namespace SeqCli.Ingestion
{
    class EnrichingReader : ILogEventReader
    {
        readonly ILogEventReader _inner;
        readonly IReadOnlyCollection<ILogEventEnricher> _enrichers;

        public EnrichingReader(
            ILogEventReader inner,
            IReadOnlyCollection<ILogEventEnricher> enrichers)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
        }

        public async Task<ReadResult> TryReadAsync()
        {
            var result = await _inner.TryReadAsync();

            if (result.LogEvent != null)
            {
                foreach (var enricher in _enrichers)
                    // We're breaking the nullability contract of `ILogEventEnricher.Enrich()`, here.
                    enricher.Enrich(result.LogEvent, null!);
            }

            return result;
        }
    }
}