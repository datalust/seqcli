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

#if WINDOWS

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.ServiceProcess;
using System.Threading.Tasks;
using SeqCli.Cli;
using SeqCli.Forwarder.ServiceProcess;

namespace SeqCli.Forwarder.Cli.Commands
{
    [Command("forwarder", "start", "Start the forwarder Windows service", IsPreview = true)]
    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    class StartCommand : Command
    {
        protected override Task<int> Run()
        {
            try
            {
                var controller = new ServiceController(SeqCliForwarderWindowsService.WindowsServiceName);
                if (controller.Status != ServiceControllerStatus.Stopped)
                {
                    Console.WriteLine("Cannot start {0}, current status is: {1}", controller.ServiceName, controller.Status);
                    return Task.FromResult(-1);
                }

                Console.WriteLine("Starting {0}...", controller.ServiceName);
                controller.Start();

                if (controller.Status != ServiceControllerStatus.Running)
                {
                    Console.WriteLine("Waiting up to 15 seconds for the service to start (currently: " + controller.Status + ")...");
                    controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                }

                if (controller.Status == ServiceControllerStatus.Running)
                {
                    Console.WriteLine("Started.");
                    return Task.FromResult(0);
                }
                
                Console.WriteLine("The service hasn't started successfully.");
                return Task.FromResult(-1);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException.Message);
                return Task.FromResult(-1);
            }
        }
    }
}

#endif
