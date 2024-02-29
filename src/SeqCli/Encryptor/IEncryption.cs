namespace SeqCli.Encryptor;

public interface IEncryption
{
    public byte[] Encrypt(byte[] unencrypted);
    public byte[] Decrypt(byte[] encrypted);
}