// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Forwarder.Diagnostics;
using SeqCli.Forwarder.Storage;
using SeqCli.Ingestion;
using Serilog;

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
            try
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
                    
                    ArrayPool<byte>.Shared.Return(entry.Data.Array!);
                }
            }
            catch (Exception ex)
            {
                // We don't loop here; the exception was unexpected, so it's either hard cancellation or an
                // unknown condition that could cause CPU-burning hot looping.
                Log.ForContext<ForwardingChannel>().Fatal(ex, "Forwarding ingest reader failed and exited");
            }
        }, cancellationToken: hardCancel);

        _readWorker = Task.Run(async () =>
        {
            try
            {
                if (bookmark.TryGet(out var bookmarkValue))
                {
                    reader.AdvanceTo(bookmarkValue.Value);
                }
                
                // Stopping shipping is a priority during shut-down, the work represented by the persistent buffer is unbounded
                // so leaving it un-shipped avoids messier hard cancellation if we can't complete the work in time.
                while (!_stop.IsCancellationRequested)
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
            }
            catch (Exception ex)
            {
                // We don't loop here; the exception was unexpected, so it's either hard cancellation or an
                // unknown condition that could cause CPU-burning hot looping.
                Log.ForContext<ForwardingChannel>().Fatal(ex, "Forwarding log shipper failed and exited");
            }
        }, cancellationToken: hardCancel);
    }

    public async Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _hardCancel);

        var copyBuffer = ArrayPool<byte>.Shared.Rent(data.Count);
        data.AsSpan().CopyTo(copyBuffer.AsSpan());
        await _writer.WriteAsync(new ForwardingChannelEntry(new ArraySegment<byte>(copyBuffer, 0, data.Count), tcs), cts.Token);
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
