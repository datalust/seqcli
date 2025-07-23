// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Filesystem.System;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class TransparentForwardingAuthenticationStrategy : ForwardingAuthenticationStrategy
{
    readonly Lock _channelsSync = new();
    readonly Dictionary<string, ForwardingChannel> _channelsByApiKey = new();
    const string EmptyApiKeyChannelId = "EmptyApiKey";

    public TransparentForwardingAuthenticationStrategy(string bufferPath, SeqConnection connection, SeqCliConfig config) : base(bufferPath, connection, config)
    {
        LoadChannels();
    }
    
    // Start forwarding channels found on the file system.
    void LoadChannels()
    {
        foreach (var directoryPath in Directory.EnumerateDirectories(BufferPath))
        {
            if (directoryPath.Equals(GetStorePath(SharedConnectionForwardingAuthenticationStrategy.ChannelId)))
            {
                Log.ForContext<TransparentForwardingAuthenticationStrategy>().Information(
                    "Ignoring data stored in `{DirectoryPath}` prior to API key forwarding being enabled", directoryPath);
                continue;
            }

            string apiKey, channelId;

            if (new SystemStoreDirectory(directoryPath).TryReadApiKey(Config, out var key))
            {
                apiKey = key;
                channelId = directoryPath;
            }
            else
            {
                Log.ForContext<TransparentForwardingAuthenticationStrategy>().Information(
                    "Directory `{DirectoryPath}` does not contain a readable API key and will be ignored", directoryPath);
                continue;
            }
            
            var created = OpenOrCreateChannel(channelId, apiKey);
            _channelsByApiKey.Add(apiKey, created);
        }
    }

    public override ForwardingChannel GetForwardingChannel(string? requestApiKey)
    {
        // Use an empty string to represent no api key, since `_channelsByApiKey` does not allow null keys.
        requestApiKey = string.IsNullOrWhiteSpace(requestApiKey) ? "" : requestApiKey;

        lock (_channelsSync)
        {
            if (_channelsByApiKey.TryGetValue(requestApiKey, out var channel))
            {
                return channel;
            }

            var channelId = requestApiKey == "" ? EmptyApiKeyChannelId : Guid.NewGuid().ToString("n");
            var created = OpenOrCreateChannel(channelId, requestApiKey);
            var store = new SystemStoreDirectory(GetStorePath(channelId));
            store.WriteApiKey(Config, requestApiKey);
            _channelsByApiKey.Add(requestApiKey, created);
            return created;
        }
    }

    public override async Task StopAsync()
    {
        Log.ForContext<TransparentForwardingAuthenticationStrategy>().Information("Flushing log buffers");
        OnStopping();

        Task[] stopChannels;
        lock (_channelsSync)
        {
            stopChannels = _channelsByApiKey.Values.Select(ch => ch.StopAsync()).ToArray();
        }
        
        await Task.WhenAll(stopChannels);
        await OnStoppedAsync();
    }
}
