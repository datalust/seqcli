using System;
using System.IO;
using System.Threading.Tasks;
using SeqCli.Forwarder.Filesystem.System;
using SeqCli.Forwarder.Storage;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class LogChannelMap
{
    readonly BufferAppender _defaultAppender;
    readonly LogChannel _defaultChannel;
    
    public LogChannelMap(string bufferPath)
    {
        var defaultStore = new SystemStoreDirectory(bufferPath);
        Log.Information("Opening local buffer in {BufferPath}", bufferPath);
        
        _defaultAppender = BufferAppender.Open(defaultStore);
        _defaultChannel = new LogChannel((chunk, _) =>
        {
            // TODO: chunk sizes, max chunks, ingestion log
            _defaultAppender.TryAppend(chunk.AsSpan(), 100_000_000);
            return Task.CompletedTask;
        });
    }
    
    public LogChannel Get(string? apiKey)
    {
        // apiKey is ignored.
        return _defaultChannel;
    }

    public async Task StopAsync()
    {
        Log.Information("Flushing log buffers");
        await _defaultChannel.StopAsync();
        _defaultAppender.Dispose();
    }
}
