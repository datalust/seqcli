using System;
using System.Security.Cryptography;

namespace SeqCli.Util;

static class PasswordHash
{
    const int SaltSize = 16,
        HashSize = 64,
        HashIter = 500_000;

    public static byte[] GenerateSalt()
    {
        var salt = new byte[SaltSize];
        using var cp = RandomNumberGenerator.Create();
        cp.GetBytes(salt);
        return salt;
    }

    public static byte[] Calculate(string password, byte[] salt)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (salt == null) throw new ArgumentNullException(nameof(salt));

        return Rfc2898DeriveBytes.Pbkdf2(password, salt, HashIter, HashAlgorithmName.SHA512, HashSize);
    }
}