using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Filesystem.System;
using SeqCli.Forwarder.Storage;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class ForwardingChannelMap
{
    readonly string _bufferPath;
    readonly SeqConnection _connection;
    readonly SeqCliConfig _config;
    readonly ForwardingChannel _defaultChannel;
    readonly Lock _channelsSync = new();
    readonly Dictionary<string, ForwardingChannel> _channels = new();
    readonly CancellationTokenSource _shutdownTokenSource = new();

    public ForwardingChannelMap(string bufferPath, SeqConnection connection, string? defaultApiKey, SeqCliConfig config)
    {
        _bufferPath = bufferPath;
        _connection = connection;
        _config = config;
        _defaultChannel = OpenOrCreateChannel(defaultApiKey, "Default");
        
        // TODO, load other channels at start-up
    }

    ForwardingChannel OpenOrCreateChannel(string? apiKey, string name)
    {
        // TODO, when it's not the default, persist the API key and validate equality on reopen
        
        var storePath = Path.Combine(_bufferPath, name);
        var store = new SystemStoreDirectory(storePath);
        Log.ForContext<ForwardingChannelMap>().Information("Opening local buffer in {StorePath}", storePath);
        
        return new ForwardingChannel(
            BufferAppender.Open(store),
            BufferReader.Open(store),
            Bookmark.Open(store),
            _connection,
            apiKey,
            _config.Forwarder.Storage.TargetChunkSizeBytes,
            _config.Forwarder.Storage.MaxChunks,
            _config.Connection.BatchSizeLimitBytes,
            _shutdownTokenSource.Token);
    }

    public ForwardingChannel Get(string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return _defaultChannel;
        }
        
        lock (_channelsSync)
        {
            if (_channels.TryGetValue(apiKey, out var channel))
            {
                return channel;
            }

            // Seq API keys begin with four identifying characters that aren't considered part of the
            // confidential key. TODO: we could likely do better than this.
            var name = apiKey[..4];
            var created = OpenOrCreateChannel(apiKey, name);
            _channels.Add(apiKey, created);
            return created;
        }
    }

    public async Task StopAsync()
    {
        Log.ForContext<ForwardingChannelMap>().Information("Flushing log buffers");
        
        _shutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        Task[] stopChannels;
        lock (_channelsSync)
        {
            stopChannels = _channels.Values.Select(ch => ch.StopAsync()).ToArray();
        }
        
        await Task.WhenAll([
            _defaultChannel.StopAsync(),
            ..stopChannels]);
        
        await _shutdownTokenSource.CancelAsync(); 
    }
}