namespace SeqCli.Encryptor;

class PlaintextDataProtector : IDataProtector
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