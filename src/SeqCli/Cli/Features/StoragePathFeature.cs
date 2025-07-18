using System;
using System.IO;

#if WINDOWS
using SeqCli.Forwarder.ServiceProcess;
#endif

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
        
    public string ConfigFilePath => Path.Combine(StorageRootPath, "SeqCli.json");

    public string BufferPath => Path.Combine(StorageRootPath, "SeqCli", "Buffer");

    public override void Enable(OptionSet options)
    {
        options.Add("storage=",
            "The folder where `SeqCli.json` and other data will be stored; " +
            "`" + GetDefaultStorageRoot() + "` is used by default",
            v => _storageRoot = Path.GetFullPath(v));
    }

    static string GetDefaultStorageRoot()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    static string? TryQueryInstalledStorageRoot()
    {
#if WINDOWS
        if (Forwarder.Util.ServiceConfiguration.GetServiceStoragePath(
            SeqCliForwarderWindowsService.WindowsServiceName, out var storage))
            return storage;
#endif
            
        return null;
    }
}