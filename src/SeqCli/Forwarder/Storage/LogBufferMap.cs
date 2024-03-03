using System;
using System.Threading.Tasks;

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
}
