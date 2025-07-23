using System;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class SeqCliConnectionForwardingChannelWrapper: ForwardingChannelWrapper
{
    readonly ForwardingChannel _seqCliConnectionChannel;
    
    public SeqCliConnectionForwardingChannelWrapper(string bufferPath, SeqConnection connection, SeqCliConfig config, string? seqCliApiKey): base(bufferPath, connection, config)
    {
        _seqCliConnectionChannel = OpenOrCreateChannel(SeqCliConnectionChannelName, seqCliApiKey);
    }
    
    public override ForwardingChannel GetForwardingChannel(string? _)
    {
        return _seqCliConnectionChannel;
    }
    
    public override async Task StopAsync()
    {
        Log.ForContext<SeqCliConnectionForwardingChannelWrapper>().Information("Flushing log buffers");
        ShutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        await _seqCliConnectionChannel.StopAsync();
        await ShutdownTokenSource.CancelAsync(); 
    }
}