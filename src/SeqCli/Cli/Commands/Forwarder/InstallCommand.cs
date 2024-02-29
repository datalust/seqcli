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
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using Seq.Forwarder.Cli.Features;
using Seq.Forwarder.Config;
using Seq.Forwarder.ServiceProcess;
using Seq.Forwarder.Util;

// ReSharper disable once ClassNeverInstantiated.Global

namespace Seq.Forwarder.Cli.Commands
{
    [Command("forwarder", "install", "Install the Seq Forwarder as a Windows service")]
    class InstallCommand : Command
    {
        readonly StoragePathFeature _storagePath;
        readonly ServiceCredentialsFeature _serviceCredentials;
        readonly ListenUriFeature _listenUri;

        bool _setup;

        public InstallCommand()
        {
            _storagePath = Enable<StoragePathFeature>();
            _listenUri = Enable<ListenUriFeature>();
            _serviceCredentials = Enable<ServiceCredentialsFeature>();

            Options.Add(
                "setup",
                "Install and start the service only if it does not exist; otherwise reconfigure the binary location",
                v => _setup = true);
        }

        string ServiceUsername => _serviceCredentials.IsUsernameSpecified ? _serviceCredentials.Username : "NT AUTHORITY\\LocalService";

        protected override int Run(TextWriter cout)
        {
            try
            {
                if (!_setup)
                {
                    Install(cout);
                    return 0;
                }

                var exit = Setup(cout);
                if (exit == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Setup completed successfully.");
                    Console.ResetColor();
                }
                return exit;
            }
            catch (DirectoryNotFoundException dex)
            {
                cout.WriteLine("Could not install the service, directory not found: " + dex.Message);
                return -1;
            }
            catch (Exception ex)
            {
                cout.WriteLine("Could not install the service: " + ex.Message);
                return -1;
            }
        }
        
        int Setup(TextWriter cout)
        {
            ServiceController controller;
            try
            {
                cout.WriteLine("Checking the status of the Seq Forwarder service...");

                controller = new ServiceController(SeqForwarderWindowsService.WindowsServiceName);
                cout.WriteLine("Status is {0}", controller.Status);
            }
            catch (InvalidOperationException)
            {
                Install(cout);
                var controller2 = new ServiceController(SeqForwarderWindowsService.WindowsServiceName);
                return Start(controller2, cout);
            }

            cout.WriteLine("Service is installed; checking path and dependency configuration...");
            Reconfigure(controller, cout);

            if (controller.Status != ServiceControllerStatus.Running)
                return Start(controller, cout);

            return 0;
        }

        static void Reconfigure(ServiceController controller, TextWriter cout)
        {
            var sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");
            if (0 != CaptiveProcess.Run(sc, "config \"" + controller.ServiceName + "\" depend= Winmgmt/Tcpip/CryptSvc", cout.WriteLine, cout.WriteLine))
                cout.WriteLine("Could not reconfigure service dependencies; ignoring.");

            if (!ServiceConfiguration.GetServiceBinaryPath(controller, cout, out var path))
                return;

            var current = "\"" + Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, Program.BinaryName) + "\"";
            if (path.StartsWith(current))
                return;

            var seqRun = path.IndexOf(Program.BinaryName + "\" run", StringComparison.OrdinalIgnoreCase);
            if (seqRun == -1)
            {
                cout.WriteLine("Current binary path is an unrecognized format.");
                return;
            }

            cout.WriteLine("Existing service binary path is: {0}", path);

            var trimmed = path.Substring((seqRun + Program.BinaryName + " ").Length);
            var newPath = current + trimmed;
            cout.WriteLine("Updating service binary path configuration to: {0}", newPath);

            var escaped = newPath.Replace("\"", "\\\"");
            if (0 != CaptiveProcess.Run(sc, "config \"" + controller.ServiceName + "\" binPath= \"" + escaped + "\"", cout.WriteLine, cout.WriteLine))
            {
                cout.WriteLine("Could not reconfigure service path; ignoring.");
                return;
            }

            cout.WriteLine("Service binary path reconfigured successfully.");
        }

        static int Start(ServiceController controller, TextWriter cout)
        {
            controller.Start();

            if (controller.Status != ServiceControllerStatus.Running)
            {
                cout.WriteLine("Waiting up to 60 seconds for the service to start (currently: " + controller.Status + ")...");
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
            }

            if (controller.Status == ServiceControllerStatus.Running)
            {
                cout.WriteLine("Started.");
                return 0;
            }

            cout.WriteLine("The service hasn't started successfully.");
            return -1;
        }

        [DllImport("shlwapi.dll")]
        static extern bool PathIsNetworkPath(string pszPath);

        void Install(TextWriter cout)
        {
            cout.WriteLine("Installing service...");

            if (PathIsNetworkPath(_storagePath.StorageRootPath))
                throw new ArgumentException("Seq requires a local (or SAN) storage location; network shares are not supported.");

            cout.WriteLine($"Updating the configuration in {_storagePath.ConfigFilePath}...");
            var config = SeqForwarderConfig.ReadOrInit(_storagePath.ConfigFilePath);
            
            if (!string.IsNullOrEmpty(_listenUri.ListenUri))
            {
                config.Api.ListenUri = _listenUri.ListenUri;
                SeqForwarderConfig.Write(_storagePath.ConfigFilePath, config);
            }

            if (_serviceCredentials.IsUsernameSpecified)
            {
                if (!_serviceCredentials.IsPasswordSpecified)
                    throw new ArgumentException(
                        "If a service user account is specified, a password for the account must also be specified.");

                // https://technet.microsoft.com/en-us/library/cc794944(v=ws.10).aspx
                cout.WriteLine($"Ensuring {_serviceCredentials.Username} is granted 'Log on as a Service' rights...");
                AccountRightsHelper.EnsureServiceLogOnRights(_serviceCredentials.Username);
            }

            cout.WriteLine($"Granting {ServiceUsername} rights to {_storagePath.StorageRootPath}...");
            GiveFullControl(_storagePath.StorageRootPath);

            cout.WriteLine($"Granting {ServiceUsername} rights to {config.Diagnostics.InternalLogPath}...");
            GiveFullControl(config.Diagnostics.InternalLogPath);

            var listenUri = MakeListenUriReservationPattern(config.Api.ListenUri);
            cout.WriteLine($"Adding URL reservation at {listenUri} for {ServiceUsername}...");
            var netshResult = CaptiveProcess.Run("netsh", $"http add urlacl url={listenUri} user=\"{ServiceUsername}\"", cout.WriteLine, cout.WriteLine);
            if (netshResult != 0)
                cout.WriteLine($"Could not add URL reservation for {listenUri}: `netsh` returned {netshResult}; ignoring");

            var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, Program.BinaryName);
            var forwarderRunCmdline = $"\"{exePath}\" run --storage=\"{_storagePath.StorageRootPath}\"";

            var binPath = forwarderRunCmdline.Replace("\"", "\\\"");

            var scCmdline = "create \"" + SeqForwarderWindowsService.WindowsServiceName + "\"" +
                            " binPath= \"" + binPath + "\"" +
                            " start= auto" +
                            " depend= Winmgmt/Tcpip/CryptSvc";

            if (_serviceCredentials.IsUsernameSpecified)
                scCmdline += $" obj= {_serviceCredentials.Username} password= {_serviceCredentials.Password}";

            var sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");
            if (0 != CaptiveProcess.Run(sc, scCmdline, cout.WriteLine, cout.WriteLine))
            {
                throw new ArgumentException("Service setup failed");
            }

            cout.WriteLine("Setting service restart policy...");
            if (0 != CaptiveProcess.Run(sc, $"failure \"{SeqForwarderWindowsService.WindowsServiceName}\" actions= restart/60000/restart/60000/restart/60000// reset= 600000", cout.WriteLine, cout.WriteLine))
                cout.WriteLine("Could not set service restart policy; ignoring");
            cout.WriteLine("Setting service description...");
            if (0 != CaptiveProcess.Run(sc, $"description \"{SeqForwarderWindowsService.WindowsServiceName}\" \"Durable storage and forwarding of application log events\"", cout.WriteLine, cout.WriteLine))
                cout.WriteLine("Could not set service description; ignoring");

            cout.WriteLine("Service installed successfully.");
        }

        void GiveFullControl(string target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (!Directory.Exists(target))
                Directory.CreateDirectory(target);

            var storageInfo = new DirectoryInfo(target);
            var storageAccessControl = storageInfo.GetAccessControl();
            storageAccessControl.AddAccessRule(new FileSystemAccessRule(ServiceUsername,
                FileSystemRights.FullControl, AccessControlType.Allow));
            storageInfo.SetAccessControl(storageAccessControl);
        }

        static string MakeListenUriReservationPattern(string uri)
        {
            var listenUri = uri.Replace("localhost", "+");
            if (!listenUri.EndsWith("/"))
                listenUri += "/";
            return listenUri;
        }
    }
}

#endif
