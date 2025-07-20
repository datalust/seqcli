namespace SeqCli.Encryptor;

public interface IDataProtector
{
    public byte[] Encrypt(byte[] unencrypted);
    public byte[] Decrypt(byte[] encrypted);
}