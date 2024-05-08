using System;
using System.Threading.Tasks;
using Serilog;

namespace SeqCli.Forwarder.Storage;

class LogBufferMap
{
    public LogBufferMap()
    {
        
    }
    
    public LogBuffer Get(string? apiKey)
    {
        return new LogBuffer(async (c) => await Task.Delay(TimeSpan.FromSeconds(1), c), default);
    }

    public Task StopAsync()
    {
        Log.Information("Flushing log buffers");
        return Task.CompletedTask;
    }
}
