using System;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SeqCli.Ingestion;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Sample.Ingestion;

/// <summary>
/// Bridges the sample simulation's Serilog-based event generation into the
/// JSON-document-based shipping pipeline.
/// </summary>
class BufferingSink: ILogEventSink, IEventReader, IDisposable
{
    readonly ConcurrentQueue<JsonObject> _queue = new();
    const int QueueCapacity = 10000;
    volatile bool _disposed;

    public void Emit(LogEvent logEvent)
    {   // No problem if this is racy - we can afford a bit of extra queue space.
        if (_disposed || _queue.Count > QueueCapacity)
            return;

        var document = MetricsMapping.TryGetMetricSampleJson(logEvent, out var sample)
            ? sample
            : SerilogEventJson.ToEventJson(logEvent);

        _queue.Enqueue(document);
    }

    public Task<ReadResult> TryReadAsync()
    {
        if (!_queue.TryDequeue(out var document))
            return Task.FromResult(new ReadResult(null, _disposed));

        return Task.FromResult(new ReadResult(document, _disposed));
    }

    public void Dispose()
    {
        // No problem if this is racy and we end up with leftovers in the queue.
        _disposed = true;
        _queue.Clear();
    }
}
