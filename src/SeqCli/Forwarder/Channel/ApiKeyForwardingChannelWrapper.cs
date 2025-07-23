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
    readonly Dictionary<string, ForwardingChannel> _channelsByName = new();
    const string EmptyApiKeyChannelName = "EmptyApiKey";

    public ApiKeyForwardingChannelWrapper(string bufferPath, SeqConnection connection, SeqCliConfig config) : base(bufferPath, connection, config)
    {
        LoadChannels();
    }
    
    // Start forwarding channels found on the file system.
    void LoadChannels()
    {
        foreach (var directoryPath in Directory.EnumerateDirectories(BufferPath))
        {
            if (directoryPath.Equals(GetStorePath(SeqCliConnectionChannelName)))
            {
                // data was stored when not using API key forwarding
                continue;
            }

            string apiKey, channelName;
            
            if (directoryPath.Equals(GetStorePath(EmptyApiKeyChannelName)))
            {
                channelName = EmptyApiKeyChannelName;
                apiKey = "";
            }
            else
            {
                if (new SystemStoreDirectory(directoryPath).TryReadApiKey(Config, out var key))
                {
                    apiKey = key!;
                    channelName = ApiKeyToName(apiKey);
                }
                else
                {
                    // directory should contain an api key file but does not
                    continue;
                }
            }
            
            var created = OpenOrCreateChannel(channelName, apiKey);
            _channelsByName.Add(channelName, created);
        }
    }

    public override ForwardingChannel GetForwardingChannel(string? requestApiKey)
    {
        lock (ChannelsSync)
        {
            var channelName = ApiKeyToName(requestApiKey);

            if (channelName == SeqCliConnectionChannelName)
            {
                // being defensive - this can't happen.
                throw new ArgumentException("Request API key is invalid");
            }
            
            if (_channelsByName.TryGetValue(channelName, out var channel))
            {
                return channel;
            }

            var created = OpenOrCreateChannel(channelName, requestApiKey);
            var store = new SystemStoreDirectory(GetStorePath(channelName));
            if (requestApiKey != null)
            {
                store.WriteApiKey(Config, requestApiKey);
            }
            _channelsByName.Add(channelName, created);
            return created;
        }
    }

    string ApiKeyToName(string? apiKey)
    {
        // Seq API keys begin with four identifying characters that aren't considered part of the
        // confidential key. TODO: we could likely do better than this.
        return string.IsNullOrEmpty(apiKey) ? EmptyApiKeyChannelName : apiKey[..(Math.Min(apiKey.Length, 4))];
    }

    public override async Task StopAsync()
    {
        Log.ForContext<ApiKeyForwardingChannelWrapper>().Information("Flushing log buffers");
        ShutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));

        Task[] stopChannels;
        lock (ChannelsSync)
        {
            stopChannels = _channelsByName.Values.Select(ch => ch.StopAsync()).ToArray();
        }
        
        await Task.WhenAll([..stopChannels]);
        await ShutdownTokenSource.CancelAsync(); 
    }
}