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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Filesystem.System;
using SeqCli.Forwarder.Storage;
using Serilog;

namespace SeqCli.Forwarder.Channel;

abstract class ForwardingAuthenticationStrategy(string bufferPath, SeqConnection connection, SeqCliConfig config)
{
    readonly CancellationTokenSource _shutdownTokenSource = new();

    protected readonly string BufferPath = bufferPath;
    protected readonly SeqCliConfig Config = config;

    // <param name="id">The id used for the channel storage on the file system.</param>
    // <param name="apiKey">The apiKey that will be used to connect to the downstream Seq instance.</param>
    protected ForwardingChannel OpenOrCreateChannel(string id, string? apiKey)
    {
        var storePath = GetStorePath(id);
        var store = new SystemStoreDirectory(storePath);
        
        Log.ForContext<ForwardingAuthenticationStrategy>().Information("Opening local buffer in {StorePath}", storePath);
        
        return new ForwardingChannel(
            BufferAppender.Open(store),
            BufferReader.Open(store),
            Bookmark.Open(store),
            connection,
            apiKey,
            Config.Forwarder.Storage.TargetChunkSizeBytes,
            Config.Forwarder.Storage.MaxChunks,
            Config.Connection.BatchSizeLimitBytes,
            _shutdownTokenSource.Token);
    }
    
    public abstract ForwardingChannel GetForwardingChannel(string? requestApiKey);

    public abstract Task StopAsync();
    
    protected async Task OnStoppedAsync()
    {
        await _shutdownTokenSource.CancelAsync();
    }

    protected void OnStopping()
    {
        _shutdownTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
    }

    protected string GetStorePath(string id)
    {
        return Path.Combine(BufferPath, id);
    }
}
