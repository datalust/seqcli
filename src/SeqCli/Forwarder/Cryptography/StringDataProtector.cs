#if WINDOWS
using Seq.Forwarder.Cryptography;
#endif

namespace SeqCli.Forwarder.Cryptography;

static class StringDataProtector
{
    public static IStringDataProtector CreatePlatformDefault()
    {
#if WINDOWS
            return new DpapiMachineScopeDataProtect();
#else
        return new UnprotectedStringData();
#endif
    }
}