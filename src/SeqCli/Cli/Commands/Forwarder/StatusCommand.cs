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
using System.IO;
using System.ServiceProcess;
using Seq.Forwarder.ServiceProcess;

namespace Seq.Forwarder.Cli.Commands
{
    [Command("forwarder", "status", "Show the status of the Seq Forwarder service")]
    class StatusCommand : Command
    {
        protected override int Run(TextWriter cout)
        {
            try
            {
                var controller = new ServiceController(SeqForwarderWindowsService.WindowsServiceName);
                cout.WriteLine("The Seq Forwarder service is installed and {0}.", controller.Status.ToString().ToLowerInvariant());
            }
            catch (InvalidOperationException)
            {
                cout.WriteLine("The Seq Forwarder service is not installed.");
            }
            catch (Exception ex)
            {
                cout.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    cout.WriteLine(ex.InnerException.Message);
                return 1;
            }

            return 0;
        }
    }
}

#endif
