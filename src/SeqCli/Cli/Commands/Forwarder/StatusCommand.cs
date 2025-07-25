﻿// Copyright © Datalust Pty Ltd
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
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using System.Threading.Tasks;
using SeqCli.Forwarder.ServiceProcess;

namespace SeqCli.Cli.Commands.Forwarder;

[Command("forwarder", "status", "Show the status of the forwarder Windows service", Visibility = FeatureVisibility.Preview, Platforms = SupportedPlatforms.Windows)]
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
class StatusCommand : Command
{
    protected override Task<int> Run()
    {
        try
        {
            var controller = new ServiceController(SeqCliForwarderWindowsService.WindowsServiceName);
            Console.WriteLine($"The {SeqCliForwarderWindowsService.WindowsServiceName} service is installed and {controller.Status.ToString().ToLowerInvariant()}.");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"The {SeqCliForwarderWindowsService.WindowsServiceName} service is not installed.");
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
