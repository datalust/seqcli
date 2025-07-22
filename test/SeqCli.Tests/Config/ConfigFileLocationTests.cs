using System;
using System.IO;
using SeqCli.Cli.Features;
using Xunit;

namespace SeqCli.Tests.Config;

public class ConfigFileLocationTests
{
    [Fact]
    public void DefaultConfigFilename()
    {
        var storagePathFeature = new StoragePathFeature(_ => null);

        // GetDefaultStorageRoot() isn't exposed by StoragePathFeature because it would introduce the risk we'd
        // use it accidentally elsewhere.
        var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        Assert.Equal(defaultPath, storagePathFeature.StorageRootPath);
    }

    [Fact]
    public void EnvironmentOverridenConfigFilename()
    {
        var customStoragePath = Path.GetTempPath();
        
        var storagePathFeature = new StoragePathFeature(
            name => "SEQCLI_STORAGE_PATH".Equals(name, StringComparison.OrdinalIgnoreCase)
                ? customStoragePath 
                : null);
        
        Assert.Equal(customStoragePath, storagePathFeature.StorageRootPath);
    }
}
