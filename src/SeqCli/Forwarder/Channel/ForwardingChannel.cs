using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Forwarder.Storage;
using SeqCli.Ingestion;

namespace SeqCli.Forwarder.Channel;

class ForwardingChannel
{
    readonly ChannelWriter<ForwardingChannelEntry> _writer;
    readonly Task _writeWorker, _readWorker;
    readonly CancellationTokenSource _stop;
    readonly CancellationToken _hardCancel;
    
    public ForwardingChannel(BufferAppender appender, BufferReader reader, Bookmark bookmark, SeqConnection connection, string? apiKey, CancellationToken hardCancel)
    {
        var channel = System.Threading.Channels.Channel.CreateBounded<ForwardingChannelEntry>(new BoundedChannelOptions(5)
        {
            SingleReader = false,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });

        _stop = CancellationTokenSource.CreateLinkedTokenSource(_hardCancel);
        _hardCancel = hardCancel;
        _writer = channel.Writer;
        _writeWorker = Task.Run(async () =>
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(hardCancel))
            {
                try
                {
                    // TODO: chunk sizes, max chunks, ingestion log
                    appender.TryAppend(entry.Data.AsSpan(), 100_000_000);
                    entry.CompletionSource.SetResult();
                }
                catch (Exception e)
                {
                    entry.CompletionSource.TrySetException(e);
                }
            }
        }, cancellationToken: hardCancel);

        _readWorker = Task.Run<Task>(async () =>
        {
            if (bookmark.TryGet(out var bookmarkValue))
            {
                reader.AdvanceTo(bookmarkValue.Value);
            }
            
            while (true)
            {
                if (_hardCancel.IsCancellationRequested) return;

                if (!reader.TryFillBatch(1024 * 1024, out var batch))
                {
                    await Task.Delay(100, hardCancel);
                    continue;
                }

                await LogShipper.ShipBuffer(connection, apiKey, batch.Value.AsArraySegment(), SendFailureHandling.Retry);

                if (bookmark.TrySet(new BufferPosition(batch.Value.ReaderHead.ChunkId,
                        batch.Value.ReaderHead.Offset)))
                {
                    reader.AdvanceTo(batch.Value.ReaderHead);
                }

                batch.Value.Return();
            }
        }, cancellationToken: hardCancel);
    }

    public async Task WriteAsync(byte[] storage, Range range, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _hardCancel);

        await _writer.WriteAsync(new ForwardingChannelEntry(storage[range], tcs), cts.Token);
        await tcs.Task;
    }

    public async Task StopAsync()
    {
        await _stop.CancelAsync();
        
        _writer.Complete();
        await _writeWorker;
        
        await _readWorker;
    }
}
