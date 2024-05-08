using System;
using System.Collections.Generic;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Tests.Forwarder.Filesystem;

public class InMemoryStoreFileAppender : StoreFileAppender
{
    readonly List<byte> _incoming;

    readonly InMemoryStoreFile _storeFile;

    public InMemoryStoreFileAppender(InMemoryStoreFile storeFile)
    {
        _storeFile = storeFile;
        _incoming = new List<byte>();
    }

    public override void Append(Span<byte> data)
    {
        _incoming.AddRange(data);
    }

    public override long Commit()
    {
        _storeFile.Append(_incoming.ToArray());
        _incoming.Clear();

        return _storeFile.Contents.Length;
    }

    public override void Sync()
    {
    }

    public override void Dispose()
    {
    }
}
