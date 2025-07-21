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
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SeqCli.Forwarder.Channel;
using SeqCli.Forwarder.Diagnostics;
using Serilog;

namespace SeqCli.Forwarder.Web.Host;

class ServerService
{
    readonly IHost _host;
    readonly ForwardingChannelMap _forwardingChannelMap;
    readonly string _listenUri;

    public ServerService(IHost host, ForwardingChannelMap forwardingChannelMap, string listenUri)
    {
        _host = host;
        _forwardingChannelMap = forwardingChannelMap;
        _listenUri = listenUri;
    }

    public void Start()
    {
        try
        {
            Log.Debug("Starting HTTP server...");
                
            _host.Start();

            Log.Information("SeqCli forwarder listening on {ListenUri}", _listenUri);
            IngestionLog.Log.Debug("SeqCli forwarder is accepting events");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error running the server application");
            throw;
        }
    }

    public async Task StopAsync()
    {
        Log.Debug("Stopping HTTP server...");

        await _host.StopAsync();

        Log.Information("HTTP server stopped; flushing buffers...");

        await _forwardingChannelMap.StopAsync();
    }
}