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

using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Config;
using Serilog;

namespace SeqCli.Forwarder.Channel;

class SharedConnectionForwardingAuthenticationStrategy: ForwardingAuthenticationStrategy
{
    public const string ChannelId = "SharedConnection";

    readonly ForwardingChannel _sharedForwardingChannel;
    
    public SharedConnectionForwardingAuthenticationStrategy(string bufferPath, SeqConnection connection, SeqCliConfig config, string? seqCliApiKey): base(bufferPath, connection, config)
    {
        _sharedForwardingChannel = OpenOrCreateChannel(ChannelId, seqCliApiKey);
    }
    
    public override ForwardingChannel GetForwardingChannel(string? _)
    {
        return _sharedForwardingChannel;
    }
    
    public override async Task StopAsync()
    {
        Log.ForContext<SharedConnectionForwardingAuthenticationStrategy>().Information("Flushing log buffer");
        OnStopping();

        await _sharedForwardingChannel.StopAsync();
        await OnStoppedAsync(); 
    }
}