using System;
using System.Threading.Tasks;
using Seq.Api;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class SeqCliConnectionForwardingChannelWrapper: ForwardingChannelWrapper
{
    readonly ForwardingChannel _seqCliConnectionChannel;
    
    public SeqCliConnectionForwardingChannelWrapper(string bufferPath, SeqConnection connection, string? seqCliApiKey): base(bufferPath, connection)
    {
        _seqCliConnectionChannel = OpenOrCreateChannel(SeqCliConnectionChannelName, seqCliApiKey);
    }
    
    public override ForwardingChannel GetForwardingChannel(string? _)
    {
        return _seqCliConnectionChannel;
    }
    
    public override async Task StopAsync()
    {
        Log.Information("Flushing log buffers");
        ShutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        await _seqCliConnectionChannel.StopAsync();
        await ShutdownTokenSource.CancelAsync(); 
    }
}