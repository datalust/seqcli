#if !WINDOWS

using Serilog;

namespace SeqCli.Forwarder.Cryptography
{
    public class UnprotectedStringData : IStringDataProtector
    {
        public string Protect(string value)
        {
            Log.Warning("Data protection is not available on this platform; sensitive values will be stored in plain text");
            return value;
        }

        public string Unprotect(string @protected)
        {
            return @protected;
        }
    }
}

#endif
