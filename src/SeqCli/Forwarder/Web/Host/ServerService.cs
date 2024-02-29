// Copyright Datalust Pty Ltd
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
using Microsoft.Extensions.Hosting;
using SeqCli.Forwarder.Diagnostics;
using SeqCli.Forwarder.Multiplexing;
using Serilog;

namespace SeqCli.Forwarder.Web.Host;

class ServerService
{
    readonly ActiveLogBufferMap _logBufferMap;
    readonly IHost _host;
    readonly string _listenUri;

    public ServerService(ActiveLogBufferMap logBufferMap, IHost host, string listenUri)
    {
        _logBufferMap = logBufferMap;
        _host = host;
        _listenUri = listenUri;
    }

    public void Start()
    {
        try
        {
            Log.Debug("Starting HTTP server...");
                
            _host.Start();

            Log.Information("Seq Forwarder listening on {ListenUri}", _listenUri);
            IngestionLog.Log.Debug("Seq Forwarder is accepting events");

            _logBufferMap.Load();
            _logBufferMap.Start();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Error running the server application");
            throw;
        }
    }

    public void Stop()
    {
        Log.Debug("Seq Forwarder stopping");

        _host.StopAsync().Wait();
        _logBufferMap.Stop();

        Log.Information("Seq Forwarder stopped cleanly");
    }
}