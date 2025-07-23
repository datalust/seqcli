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
using System.Security.AccessControl;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Forwarder.Cli.Features;
using SeqCli.Forwarder.ServiceProcess;
using SeqCli.Forwarder.Util;

// ReSharper disable once ClassNeverInstantiated.Global

[Command("forwarder", "install", "Install the forwarder as a Windows service", Visibility = FeatureVisibility.Preview)]
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
class InstallCommand : Command
{
    readonly StoragePathFeature _storagePath;
    readonly ServiceCredentialsFeature _serviceCredentials;
    readonly ListenUriFeature _listenUri;
    public InstallCommand()
    {
        _storagePath = Enable<StoragePathFeature>();
        _listenUri = Enable<ListenUriFeature>();
        _serviceCredentials = Enable<ServiceCredentialsFeature>();
    }

    string ServiceUsername => _serviceCredentials.IsUsernameSpecified ? _serviceCredentials.Username : "NT AUTHORITY\\LocalService";

    protected override Task<int> Run()
    {
        try
        {
            Install();
            return Task.FromResult(0);
        }
        catch (DirectoryNotFoundException dex)
        {
            Console.WriteLine("Could not install the service, directory not found: " + dex.Message);
            return Task.FromResult(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Could not install the service: " + ex.Message);
            return Task.FromResult(-1);
        }
    }

    void Install()
    {
        Console.WriteLine("Installing service...");

        Console.WriteLine($"Updating the configuration in {_storagePath.ConfigFilePath}...");
        var config = SeqCliConfig.ReadFromFile(_storagePath.ConfigFilePath);
        
        if (!string.IsNullOrEmpty(_listenUri.ListenUri))
        {
            config.Forwarder.Api.ListenUri = _listenUri.ListenUri;
            SeqCliConfig.WriteToFile(config, _storagePath.ConfigFilePath);
        }

        if (_serviceCredentials.IsUsernameSpecified)
        {
            if (!_serviceCredentials.IsPasswordSpecified)
                throw new ArgumentException(
                    "If a service user account is specified, a password for the account must also be specified.");

            // https://technet.microsoft.com/en-us/library/cc794944(v=ws.10).aspx
            Console.WriteLine($"Ensuring {_serviceCredentials.Username} is granted 'Log on as a Service' rights...");
            AccountRightsHelper.EnsureServiceLogOnRights(_serviceCredentials.Username);
        }

        Console.WriteLine($"Granting {ServiceUsername} rights to {_storagePath.StorageRootPath}...");
        GiveFullControl(_storagePath.StorageRootPath);

        Console.WriteLine($"Granting {ServiceUsername} rights to {_storagePath.InternalLogPath}...");
        GiveFullControl(_storagePath.InternalLogPath);

        var listenUri = MakeListenUriReservationPattern(config.Forwarder.Api.ListenUri);
        Console.WriteLine($"Adding URL reservation at {listenUri} for {ServiceUsername}...");
        var netshResult = CaptiveProcess.Run("netsh", $"http add urlacl url={listenUri} user=\"{ServiceUsername}\"", Console.WriteLine, Console.WriteLine);
        if (netshResult != 0)
            Console.WriteLine($"Could not add URL reservation for {listenUri}: `netsh` returned {netshResult}; ignoring");

        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Program.BinaryName);
        var forwarderRunCmdline = $"\"{exePath}\" forwarder run --pre --storage=\"{_storagePath.StorageRootPath}\"";

        var binPath = forwarderRunCmdline.Replace("\"", "\\\"");

        var scCmdline = "create \"" + SeqCliForwarderWindowsService.WindowsServiceName + "\"" +
                        " binPath= \"" + binPath + "\"" +
                        " start= auto" +
                        " depend= Winmgmt/Tcpip/CryptSvc";

        if (_serviceCredentials.IsUsernameSpecified)
            scCmdline += $" obj= {_serviceCredentials.Username} password= {_serviceCredentials.Password}";

        var sc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "sc.exe");
        if (0 != CaptiveProcess.Run(sc, scCmdline, Console.WriteLine, Console.WriteLine))
        {
            throw new ArgumentException("Service setup failed.");
        }

        Console.WriteLine("Setting service restart policy...");
        if (0 != CaptiveProcess.Run(sc, $"failure \"{SeqCliForwarderWindowsService.WindowsServiceName}\" actions= restart/60000/restart/60000/restart/60000// reset= 600000", Console.WriteLine, Console.WriteLine))
            Console.WriteLine("Could not set service restart policy; ignoring");
        Console.WriteLine("Setting service description...");
        if (0 != CaptiveProcess.Run(sc, $"description \"{SeqCliForwarderWindowsService.WindowsServiceName}\" \"Durable storage and forwarding of application log events\"", Console.WriteLine, Console.WriteLine))
            Console.WriteLine("Could not set service description; ignoring");

        Console.WriteLine("Service installed successfully.");
    }

    void GiveFullControl(string target)
    {
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
        if (!listenUri.EndsWith('/'))
            listenUri += "/";
        return listenUri;
    }
}

#endif
