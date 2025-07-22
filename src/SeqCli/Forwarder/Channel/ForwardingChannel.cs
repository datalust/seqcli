using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Forwarder.Diagnostics;
using SeqCli.Forwarder.Storage;
using SeqCli.Ingestion;

namespace SeqCli.Forwarder.Channel;

class ForwardingChannel
{
    readonly ChannelWriter<ForwardingChannelEntry> _writer;
    readonly Task _writeWorker, _readWorker;
    readonly CancellationTokenSource _stop;
    readonly CancellationToken _hardCancel;
    
    public ForwardingChannel(BufferAppender appender, BufferReader reader, Bookmark bookmark,
        SeqConnection connection, string? apiKey, long targetChunkSizeBytes, int? maxChunks, int batchSizeLimitBytes, CancellationToken hardCancel)
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
                    const int maxTries = 3;
                    for (var retry = 0; retry < maxTries; ++retry)
                    {
                        if (appender.TryAppend(entry.Data.AsSpan(), targetChunkSizeBytes, maxChunks))
                        {
                            entry.CompletionSource.SetResult();
                            break;
                        }
                        
                        if (retry == maxTries - 1)
                        {
                            IngestionLog.Log.Error("Buffering failed due to an I/O error; the incoming chunk was rejected");
                            entry.CompletionSource.TrySetException(new IOException("Buffering failed due to an I/O error."));
                        }
                    }
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

                if (!reader.TryFillBatch(batchSizeLimitBytes, out var batch))
                {
                    await Task.Delay(100, hardCancel);
                    continue;
                }

                await LogShipper.ShipBufferAsync(connection, apiKey, batch.Value.AsArraySegment(), IngestionLog.Log, hardCancel);

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
