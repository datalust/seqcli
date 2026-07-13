using System;
using System.IO;
using SeqCli.Forwarder.Filesystem;
using SeqCli.Forwarder.Filesystem.System;

namespace SeqCli.Tests.Forwarder.Filesystem;

static class TestFilesystem
{
    public static void PermuteFilesystem(Action<StoreDirectory> withDirectory)
    {
        try
        {
            withDirectory(new InMemoryStoreDirectory());
        }
        catch (Exception e)
        {
            throw new Exception("In-memory permutation failed", e);
        }

        var tmpDir = Path.Combine(Path.GetTempPath(), $"SeqCliTest_{Guid.NewGuid():X}");
        Directory.CreateDirectory(tmpDir);
        
        try
        {
            withDirectory(new SystemStoreDirectory(tmpDir));
        }
        catch (Exception e)
        {
            throw new Exception("Filesystem permutation failed", e);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }
}
