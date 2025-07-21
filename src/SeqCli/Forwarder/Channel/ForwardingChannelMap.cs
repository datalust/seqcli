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
    const string DefaultChannelName = "Default";

    public ForwardingChannelMap(string bufferPath, SeqConnection connection, SeqCliConfig config, string? seqCliApiKey)
    {
        _bufferPath = bufferPath;
        _connection = connection;
        _config = config;
        _defaultChannel = OpenOrCreateChannel(seqCliApiKey, DefaultChannelName);
        
        ReopenApiKeyChannels();
    }

    ForwardingChannel OpenOrCreateChannel(string? apiKey, string name)
    {
        var storePath = GetStorePath(name);
        var store = new SystemStoreDirectory(storePath);
        
        Log.Information("Opening local buffer in {StorePath}", storePath);
        
        return new ForwardingChannel(
            BufferAppender.Open(store),
            BufferReader.Open(store),
            Bookmark.Open(store),
            _connection,
            apiKey,
            _shutdownTokenSource.Token);
    }
    
    void ReopenApiKeyChannels()
    {
        if (_config.Forwarder.UseApiKeyForwarding)
        {
            foreach (var directoryPath in Directory.EnumerateDirectories(_bufferPath))
            {
                if (directoryPath.Equals(GetStorePath("Default"))) continue;
                
                var path = new SystemStoreDirectory(directoryPath);
                var apiKey = path.ReadApiKey(_config);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    var created = OpenOrCreateChannel(apiKey, ApiKeyToName(apiKey));
                    
                    lock (_channelsSync)
                    {
                        _channels.Add(apiKey, created);
                    }
                }
            }
        }
    }

    string GetStorePath(string name)
    {
        return Path.Combine(_bufferPath, name);
    }

    public ForwardingChannel GetSeqCliConnectionChannel()
    {
        return _defaultChannel;
    }

    public ForwardingChannel GetApiKeyChannel(string apiKey)
    {
        lock (_channelsSync)
        {
            if (_channels.TryGetValue(apiKey, out var channel))
            {
                return channel;
            }

            var created = OpenOrCreateChannel(apiKey, ApiKeyToName(apiKey));
            var store = new SystemStoreDirectory(GetStorePath(ApiKeyToName(apiKey)));
            store.WriteApiKey(_config, apiKey);
            _channels.Add(apiKey, created);
            return created;
        }
    }

    string ApiKeyToName(string apiKey)
    {
        // Seq API keys begin with four identifying characters that aren't considered part of the
        // confidential key. TODO: we could likely do better than this.
        return apiKey[..(Math.Min(apiKey.Length, 4))];
    }

    public async Task StopAsync()
    {
        Log.Information("Flushing log buffers");
        
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