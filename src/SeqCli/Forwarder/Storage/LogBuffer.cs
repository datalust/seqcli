using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SeqCli.Forwarder.Storage;

class LogBuffer
{
    public LogBuffer(Func<CancellationToken, Task> write, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<LogBufferEntry>(new BoundedChannelOptions(5)
        {
            SingleReader = false,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait,
        });

        _shutdownTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _writer = channel.Writer;
        _worker = Task.Run(async () =>
        {
            await foreach (var entry in channel.Reader.ReadAllAsync(_shutdownTokenSource.Token))
            {
                try
                {
                    await write(_shutdownTokenSource.Token);
                    entry.Completion.SetResult();
                }
                catch (Exception e)
                {
                    entry.Completion.TrySetException(e);
                }
            }
        }, cancellationToken: _shutdownTokenSource.Token);
    }
    
    readonly ChannelWriter<LogBufferEntry> _writer;
    readonly Task _worker;
    readonly CancellationTokenSource _shutdownTokenSource;
    
    public async Task WriteAsync(byte[] storage, Range range, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdownTokenSource.Token);

        await _writer.WriteAsync(new LogBufferEntry(storage, range, tcs), cts.Token);
        await tcs.Task;
    }

    public async Task StopAsync()
    {
        _writer.Complete();
        await _worker;
        await _shutdownTokenSource.CancelAsync(); 
    }
}