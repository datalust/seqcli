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
    readonly string? _seqCliApiKey;
    
    // Either seqcli is using its usual connection details and `_seqClieConnectionChannel` is the channel,
    // or seqcli is using the incoming API key and there is one channel per API key (plus one for no API key) in the dictionary.
    readonly Lock _channelsSync = new();
    ForwardingChannel? _seqCliConnectionChannel = null;
    readonly Dictionary<string, ForwardingChannel> _channelsByName = new();
    
    readonly CancellationTokenSource _shutdownTokenSource = new();
    const string SeqCliConnectionChannelName = "SeqCliConnection";

    public ForwardingChannelMap(string bufferPath, SeqConnection connection, SeqCliConfig config, string? seqCliApiKey)
    {
        _bufferPath = bufferPath;
        _connection = connection;
        _config = config;
        _seqCliApiKey = seqCliApiKey;

        LoadChannels();
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
    
    void LoadChannels()
    {
        if (_config.Forwarder.UseApiKeyForwarding)
        {
            foreach (var directoryPath in Directory.EnumerateDirectories(_bufferPath))
            {
                if (directoryPath.Equals(GetStorePath(SeqCliConnectionChannelName)))
                {
                    // data was stored when not using API key forwarding
                    continue;
                }

                var path = new SystemStoreDirectory(directoryPath);
                var apiKey = path.ReadApiKey(_config);

                var channelName = ApiKeyToName(apiKey);
                var created = OpenOrCreateChannel(apiKey, channelName);
                _channelsByName.Add(channelName, created);
            }
        }
        else
        {
            _seqCliConnectionChannel = OpenOrCreateChannel(_seqCliApiKey, SeqCliConnectionChannelName);
        }
    }

    string GetStorePath(string name)
    {
        return Path.Combine(_bufferPath, name);
    }

    public ForwardingChannel GetApiKeyForwardingChannel(string? requestApiKey)
    {
        lock (_channelsSync)
        {
            var channelName = ApiKeyToName(requestApiKey);
            
            if (_channelsByName.TryGetValue(channelName, out var channel))
            {
                return channel;
            }

            var created = OpenOrCreateChannel(requestApiKey, channelName);
            var store = new SystemStoreDirectory(GetStorePath(channelName));
            if (requestApiKey != null)
            {
                store.WriteApiKey(_config, requestApiKey);
            }
            _channelsByName.Add(channelName, created);
            return created;
        }
    }
    
    public ForwardingChannel GetSeqCliConnectionChannel()
    {
        lock (_channelsSync)
        {
            if (_seqCliConnectionChannel == null)
            {
                _seqCliConnectionChannel = OpenOrCreateChannel(_seqCliApiKey, SeqCliConnectionChannelName);
            }
            return _seqCliConnectionChannel;
        }
    }

    string ApiKeyToName(string? apiKey)
    {
        // Seq API keys begin with four identifying characters that aren't considered part of the
        // confidential key. TODO: we could likely do better than this.
        return string.IsNullOrEmpty(apiKey) ? "EmptyApiKey" : apiKey[..(Math.Min(apiKey.Length, 4))];
    }

    public async Task StopAsync()
    {
        Log.Information("Flushing log buffers");
        
        _shutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        Task[] stopChannels;
        lock (_channelsSync)
        {
            stopChannels = _channelsByName.Values.Select(ch => ch.StopAsync()).ToArray();
        }

        if (_seqCliConnectionChannel != null)
        {
            stopChannels = stopChannels.Append(_seqCliConnectionChannel.StopAsync()).ToArray();
        }
        
        await Task.WhenAll([..stopChannels]);
        
        await _shutdownTokenSource.CancelAsync(); 
    }
}