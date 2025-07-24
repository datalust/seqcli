using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SeqCli.Encryptor;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.Config;

public class ExternalDataProtectorTests
{
    [Fact]
    public void IfEncryptorDoesNotExistEncryptThrows()
    {
        var protector = new ExternalDataProtector(Some.String(), null, Some.String(), null);
        Assert.Throws<Win32Exception>(() => protector.Encrypt(Some.Bytes(200)));
    }

    [Fact]
    public void IfEncryptorFailsEncryptThrows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        var protector = new ExternalDataProtector("bash", "-c \"exit 1\"", Some.String(), null);
        // May be `Exception` or `IOException`.
        Assert.ThrowsAny<Exception>(() => protector.Encrypt(Some.Bytes(200)));
    }

    [Fact]
    public void EncryptCallsEncryptor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        const string prefix = "123";
        
        var encoding = new UTF8Encoding(false);
        using var temp = TempFolder.ForCaller();
        var filename = temp.AllocateFilename();
        File.WriteAllBytes(filename, encoding.GetBytes(prefix));
        
        const string input = "Hello, world!";
        
        var protector = new ExternalDataProtector("bash", $"-c \"cat '{filename}' -\"", Some.String(), null);
        var actual = encoding.GetString(protector.Encrypt(encoding.GetBytes(input)));
        
        Assert.Equal($"{prefix}{input}", actual);
    }
    
    [Fact]
    public void EncryptionRoundTrips()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        const string echo = "bash";
        const string echoArgs = "-c \"cat -\"";
        var protector = new ExternalDataProtector(echo, echoArgs, echo, echoArgs);
        var expected = Some.Bytes(200);
        var actual = protector.Decrypt(protector.Encrypt(expected));
        Assert.Equal(expected, actual);
    }
}
