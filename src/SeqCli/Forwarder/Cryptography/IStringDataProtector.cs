namespace SeqCli.Forwarder.Cryptography;

public interface IStringDataProtector
{
    string Protect(string value);
    string Unprotect(string @protected);
}