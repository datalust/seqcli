using System;
using System.Runtime.InteropServices;
using SeqCli.Config;
using SeqCli.Encryptor;
using Xunit;

namespace SeqCli.Tests.Config;

public class SeqCliEncryptionProviderConfigTests
{
    [Fact]
    public void DefaultDataProtectorOnWindowsIsDpapi()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var config = new SeqCliEncryptionProviderConfig();
        var provider = config.DataProtector();
        Assert.IsType<WindowsNativeDataProtector>(provider);
    }

    [Fact]
    public void DefaultDataProtectorOnUnixIsPlaintext()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        var config = new SeqCliEncryptionProviderConfig();
        var provider = config.DataProtector();
        Assert.IsType<PlaintextDataProtector>(provider);
    }

    [Fact]
    public void SpecifyingEncryptorRequiresDecryptor()
    {
        var config = new SeqCliEncryptionProviderConfig
        {
            Encryptor = "test"
        };
        
        Assert.Throws<ArgumentException>(() => config.DataProtector());
    }

    [Fact]
    public void SpecifyingDecryptorRequiresEncryptor()
    {
        var config = new SeqCliEncryptionProviderConfig
        {
            Decryptor = "test"
        };
        
        Assert.Throws<ArgumentException>(() => config.DataProtector());
    }

    [Fact]
    public void SpecifyingEncryptorAndDecryptorActivatesExternalDataProtector()
    {
        var config = new SeqCliEncryptionProviderConfig
        {
            Encryptor = "test",
            Decryptor = "test"
        };
        
        Assert.IsType<ExternalDataProtector>(config.DataProtector());
    }
}
