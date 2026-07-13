#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Tests.Forwarder.Filesystem;

class InMemoryStoreFile : StoreFile
{
    public byte[] Contents { get; private set; } = Array.Empty<byte>();

    int _activeReaders;
    int _activeWriters;

    public override bool TryGetLength([NotNullWhen(true)] out long? length)
    {
        length = Contents.Length;
        return true;
    }

    public override void Append(Span<byte> incoming)
    {
        var newContents = new byte[Contents.Length + incoming.Length];

        Contents.CopyTo(newContents.AsSpan());
        incoming.CopyTo(newContents.AsSpan()[^incoming.Length..]);

        Contents = newContents;
    }

    public override bool TryOpenRead(long length, [NotNullWhen(true)] out StoreFileReader? reader)
    {
        reader = new InMemoryStoreFileReader(this, (int)length);
        _activeReaders++;
        
        return true;
    }

    internal void CloseReader()
    {
        _activeReaders--;
    }

    public override bool TryOpenAppend([NotNullWhen(true)] out StoreFileAppender? appender)
    {
        if (_activeWriters != 0)
        {
            throw new InvalidOperationException("Attempt to open multiple appenders to the same file");
        }
        
        appender = new InMemoryStoreFileAppender(this);
        _activeWriters++;
        
        return true;
    }

    internal void CloseAppender()
    {
        _activeWriters--;
    }

    internal bool CanDelete()
    {
        return _activeReaders == 0 && _activeWriters == 0;
    }
}
