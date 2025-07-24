using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SeqCli.Util;

namespace SeqCli.Encryptor;

class WindowsNativeDataProtector : IDataProtector
{
    public byte[] Encrypt(byte[] unencrypted)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Windows native encryption is only supported on Windows");
        
        var salt = PasswordHash.GenerateSalt();
        var data = ProtectedData.Protect(unencrypted, salt, DataProtectionScope.LocalMachine);

        return [..data, ..salt];
    }

    public byte[] Decrypt(byte[] encrypted)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Windows native encryption is only supported on Windows");
        
        var data = encrypted[..^16];
        var salt = encrypted[^16..];

        return ProtectedData.Unprotect(data, salt, DataProtectionScope.LocalMachine);
    }
}