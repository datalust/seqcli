using System;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Tests.Forwarder.Filesystem;

public class InMemoryStoreFileReader : StoreFileReader
{
    readonly int _length;

    readonly InMemoryStoreFile _storeFile;

    public InMemoryStoreFileReader(InMemoryStoreFile storeFile, int length)
    {
        _storeFile = storeFile;
        _length = length;
    }

    public override long CopyTo(Span<byte> buffer, long from = 0, long? length = null)
    {
        var span = _storeFile.Contents.AsSpan().Slice((int)from, (int?)length ?? _length);
        span.CopyTo(buffer);

        return span.Length;
    }

    public override void Dispose()
    {
    }
}
