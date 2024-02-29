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

#if WINDOWS

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using Seq.Forwarder.ServiceProcess;
using SeqCli.Cli;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("forwarder", "status", "Show the status of the forwarder Windows service")]
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    class StatusCommand : Command
    {
        protected override Task<int> Run()
        {
            try
            {
                var controller = new ServiceController(SeqForwarderWindowsService.WindowsServiceName);
                Console.WriteLine("The Seq Forwarder service is installed and {0}.", controller.Status.ToString().ToLowerInvariant());
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("The Seq Forwarder service is not installed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException.Message);
                return Task.FromResult(1);
            }

            return Task.FromResult(1);
        }
    }
}

#endif
