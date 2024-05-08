using System;
using System.Threading.Tasks;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class LogChannelMap
{
    public LogChannelMap()
    {
        
    }
    
    public LogChannel Get(string? apiKey)
    {
        return new LogChannel(async (c) => await Task.Delay(TimeSpan.FromSeconds(1), c), default);
    }

    public Task StopAsync()
    {
        Log.Information("Flushing log buffers");
        return Task.CompletedTask;
    }
}
