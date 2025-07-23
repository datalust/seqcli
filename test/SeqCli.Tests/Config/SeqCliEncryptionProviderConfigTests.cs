using System;
using System.Runtime.InteropServices;
using SeqCli.Config;
using SeqCli.Encryptor;
using Xunit;

namespace SeqCli.Tests.Config;

public class SeqCliEncryptionProviderConfigTests
{
#if WINDOWS
    [Fact]
    public void DefaultDataProtectorOnWindowsIsDpapi()
    {
        Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var config = new SeqCliEncryptionProviderConfig();
        var provider = config.DataProtector();
        Assert.IsType<WindowsNativeDataProtector>(provider);
    }
#else
    [Fact]
    public void DefaultDataProtectorOnUnixIsPlaintext()
    {
        Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        
        var config = new SeqCliEncryptionProviderConfig();
        var provider = config.DataProtector();
        Assert.IsType<PlaintextDataProtector>(provider);
    }
#endif

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