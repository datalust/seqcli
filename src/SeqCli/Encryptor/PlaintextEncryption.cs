namespace SeqCli.Encryptor;

class PlaintextEncryption : IEncryption
{
    public byte[] Encrypt(byte[] unencrypted)
    {
        return unencrypted;
    }

    public byte[] Decrypt(byte[] encrypted)
    {
        return encrypted;
    }
}