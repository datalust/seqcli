using System;
using System.IO;

#if WINDOWS
using SeqCli.Forwarder.ServiceProcess;
#endif

namespace SeqCli.Cli.Features;

class StoragePathFeature : CommandFeature
{
    const string StoragePathVarName = "SEQCLI_STORAGE_PATH";
    
    string? _storageRoot;
    readonly Func<string, string?> _getEnvironmentVariable;

    public StoragePathFeature()
    : this(Environment.GetEnvironmentVariable)
    {
    }

    public StoragePathFeature(Func<string, string?> getEnvironmentVariable)
    {
        _getEnvironmentVariable = getEnvironmentVariable;
    }

    public string StorageRootPath
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_storageRoot))
                return _storageRoot;

            if (_getEnvironmentVariable(StoragePathVarName) is {} fromVar && !string.IsNullOrWhiteSpace(fromVar))
                return fromVar;
            
            if (TryQueryInstalledStorageRoot() is { } installed)
                return installed;
            
            return GetDefaultStorageRoot();
        }
    }
        
    public string ConfigFilePath => Path.Combine(StorageRootPath, "SeqCli.json");

    public string BufferPath => Path.Combine(StorageRootPath, "SeqCli", "Buffer");

    public string InternalLogPath => Path.Combine(StorageRootPath, "SeqCli", "Logs");

    public override void Enable(OptionSet options)
    {
        options.Add("storage=",
            $"The folder where `SeqCli.json` and other data will be stored; falls back to `{StoragePathVarName}` from " +
            $"the environment, then the `seqcli forwarder` service's configured storage path (Windows only), then " +
            "`" + GetDefaultStorageRoot() + "`",
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