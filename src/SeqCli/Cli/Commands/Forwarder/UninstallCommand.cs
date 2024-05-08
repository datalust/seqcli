﻿// Copyright Datalust Pty Ltd
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
using System.IO;
using System.Threading.Tasks;
using SeqCli.Forwarder.Util;
using SeqCli.Cli;
using SeqCli.Forwarder.ServiceProcess;
using SeqCli.Forwarder.Util;

namespace SeqCli.Forwarder.Cli.Commands
{
    [Command("forwarder", "uninstall", "Uninstall the forwarder Windows service", IsPreview = true)]
    class UninstallCommand : Command
    {
        protected override Task<int> Run()
        {
            try
            {
                Console.WriteLine("Uninstalling service...");

                var sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");
                var exitCode = CaptiveProcess.Run(sc, $"delete \"{SeqCliForwarderWindowsService.WindowsServiceName}\"", Console.WriteLine, Console.WriteLine);
                if (exitCode != 0)
                    throw new InvalidOperationException($"The `sc.exe delete` call failed with exit code {exitCode}.");

                Console.WriteLine("Service uninstalled successfully.");
                return Task.FromResult(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not uninstall the service: " + ex.Message);
                return Task.FromResult(-1);
            }
        }
    }
}

#endif
