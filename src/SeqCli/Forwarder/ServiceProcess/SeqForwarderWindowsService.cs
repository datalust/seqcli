// Copyright 2020 Datalust Pty Ltd
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

#if WINDOWS

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.ServiceProcess;
using SeqCli.Forwarder.Web.Host;

namespace Seq.Forwarder.ServiceProcess
{
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    class SeqForwarderWindowsService : ServiceBase
    {
        readonly ServerService _serverService;

        public static string WindowsServiceName { get; } = "Seq Forwarder";

        public SeqForwarderWindowsService(ServerService serverService)
        {
            // Enable TLS 1.2 Support.
            // .NET Framework 4.5.2 does not have it enabled by default
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            _serverService = serverService;

            ServiceName = WindowsServiceName;
        }

        protected override void OnStart(string[] args)
        {
            _serverService.Start();
        }

        protected override void OnStop()
        {
            _serverService.Stop();
        }
    }
}

#endif
