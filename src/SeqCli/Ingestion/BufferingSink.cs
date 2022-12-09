using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Ingestion;

class BufferingSink: ILogEventSink, ILogEventReader, IDisposable
{
    readonly ConcurrentQueue<LogEvent> _queue = new();
    const int QueueCapacity = 10000;
    volatile bool _disposed;
            
    public void Emit(LogEvent logEvent)
    {   // No problem if this is racy - we can afford a bit of extra queue space.
        if (_disposed || _queue.Count > QueueCapacity)
            return;
            
        _queue.Enqueue(logEvent);
    }

    public Task<ReadResult> TryReadAsync()
    {
        if (!_queue.TryDequeue(out var logEvent))
            return Task.FromResult(new ReadResult(null, _disposed));

        return Task.FromResult(new ReadResult(logEvent, _disposed));
    }

    public void Dispose()
    {
        // No problem if this is racy and we end up with leftovers in the queue.
        _disposed = true;
        _queue.Clear();
    }
}