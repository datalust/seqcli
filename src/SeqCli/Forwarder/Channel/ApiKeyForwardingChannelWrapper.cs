using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Filesystem.System;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class ApiKeyForwardingChannelWrapper : ForwardingChannelWrapper
{
    readonly Dictionary<string, ForwardingChannel> _channelsByApiKey = new();
    const string EmptyApiKeyChannelId = "EmptyApiKey";

    public ApiKeyForwardingChannelWrapper(string bufferPath, SeqConnection connection, SeqCliConfig config) : base(bufferPath, connection, config)
    {
        LoadChannels();
    }
    
    // Start forwarding channels found on the file system.
    void LoadChannels()
    {
        foreach (var directoryPath in Directory.EnumerateDirectories(BufferPath))
        {
            if (directoryPath.Equals(GetStorePath(SeqCliConnectionChannelId)))
            {
                // data was stored when not using API key forwarding
                continue;
            }

            string apiKey, channelId;

            if (new SystemStoreDirectory(directoryPath).TryReadApiKey(Config, out var key))
            {
                apiKey = key!;
                channelId = directoryPath;
            }
            else
            {
                // directory should contain an api key file but does not
                continue;
            }
            
            var created = OpenOrCreateChannel(channelId, apiKey);
            _channelsByApiKey.Add(apiKey, created);
        }
    }

    public override ForwardingChannel GetForwardingChannel(string? requestApiKey)
    {
        lock (ChannelsSync)
        {
            // use empty string to represent no api key
            if (_channelsByApiKey.TryGetValue(requestApiKey ?? "", out var channel))
            {
                return channel;
            }

            var channelId = ApiKeyToId(requestApiKey);
            var created = OpenOrCreateChannel(channelId, requestApiKey);
            var store = new SystemStoreDirectory(GetStorePath(channelId));
            store.WriteApiKey(Config, requestApiKey ?? "");
            _channelsByApiKey.Add(requestApiKey ?? "", created);
            return created;
        }
    }

    string ApiKeyToId(string? apiKey)
    {
        return string.IsNullOrEmpty(apiKey) ? EmptyApiKeyChannelId : Guid.NewGuid().ToString();
    }

    public override async Task StopAsync()
    {
        Log.ForContext<ApiKeyForwardingChannelWrapper>().Information("Flushing log buffers");
        ShutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        Task[] stopChannels;
        lock (ChannelsSync)
        {
            stopChannels = _channelsByApiKey.Values.Select(ch => ch.StopAsync()).ToArray();
        }
        
        await Task.WhenAll([..stopChannels]);
        await ShutdownTokenSource.CancelAsync(); 
    }
}