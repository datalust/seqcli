using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Filesystem.System;
using SeqCli.Forwarder.Storage;
using Serilog;

namespace SeqCli.Forwarder.Channel;

internal abstract class ForwardingChannelWrapper(string bufferPath, SeqConnection connection, SeqCliConfig config)
{
    protected const string SeqCliConnectionChannelName = "SeqCliConnection";
    protected readonly string BufferPath = bufferPath;
    readonly SeqCliConfig _config = config;
    protected readonly CancellationTokenSource ShutdownTokenSource = new();
    protected readonly Lock ChannelsSync = new();

    // <param name="name">The name used for the channel storage on the file system and in memory.</param>
    // <param name="apiKey">The apiKey that will be used to connect to the downstream Seq instance.</param>
    protected ForwardingChannel OpenOrCreateChannel(string name, string? apiKey)
    {
        var storePath = GetStorePath(name);
        var store = new SystemStoreDirectory(storePath);
        
        Log.Information("Opening local buffer in {StorePath}", storePath);
        
        return new ForwardingChannel(
            BufferAppender.Open(store),
            BufferReader.Open(store),
            Bookmark.Open(store),
            connection,
            apiKey,
            _config.Forwarder.Storage.TargetChunkSizeBytes,
            _config.Forwarder.Storage.MaxChunks,
            _config.Connection.BatchSizeLimitBytes,
            ShutdownTokenSource.Token);
    }
    
    public abstract ForwardingChannel GetForwardingChannel(string? requestApiKey);

    public abstract Task StopAsync();
    
    protected string GetStorePath(string name)
    {
        return Path.Combine(BufferPath, name);
    }
}