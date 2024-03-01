using System;
using System.IO;
using SeqCli.Forwarder.ServiceProcess;

namespace SeqCli.Cli.Features;

class StoragePathFeature : CommandFeature
{
    string? _storageRoot;

    public string StorageRootPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_storageRoot))
                return _storageRoot;

            return TryQueryInstalledStorageRoot() ?? GetDefaultStorageRoot();
        }
    }
        
    public string ConfigFilePath => Path.Combine(StorageRootPath, "SeqForwarder.json");

    public string BufferPath => Path.Combine(StorageRootPath, "Buffer");

    public override void Enable(OptionSet options)
    {
        options.Add("s=|storage=",
            "Set the folder where data will be stored; " +
            "" + GetDefaultStorageRoot() + " is used by default.",
            v => _storageRoot = Path.GetFullPath(v));
    }

    static string GetDefaultStorageRoot()
    {
        return Path.GetFullPath(Path.Combine(
#if WINDOWS
                // Common, here, because the service may run as Local Service, which has no obvious home
                // directory.
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
#else
            // Specific to and writable by the current user.
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
#endif
            "Seq",
            "Forwarder"));
    }

    static string? TryQueryInstalledStorageRoot()
    {
#if WINDOWS
        if (Seq.Forwarder.Util.ServiceConfiguration.GetServiceStoragePath(
            SeqCliForwarderWindowsService.WindowsServiceName, out var storage))
            return storage;
#endif
            
        return null;
    }
}