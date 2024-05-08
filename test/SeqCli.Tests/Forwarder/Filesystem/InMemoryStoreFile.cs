using System;
using System.Diagnostics.CodeAnalysis;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Tests.Forwarder.Filesystem;

public class InMemoryStoreFile : StoreFile
{
    public byte[] Contents { get; private set; } = Array.Empty<byte>();

    public override bool TryGetLength([NotNullWhen(true)] out long? length)
    {
        length = Contents.Length;
        return true;
    }

    public void Append(Span<byte> incoming)
    {
        var newContents = new byte[Contents.Length + incoming.Length];

        Contents.CopyTo(newContents.AsSpan());
        incoming.CopyTo(newContents.AsSpan()[^incoming.Length..]);

        Contents = newContents;
    }

    public override bool TryOpenRead(long length, [NotNullWhen(true)] out StoreFileReader? reader)
    {
        reader = new InMemoryStoreFileReader(this, (int)length);
        return true;
    }

    public override bool TryOpenAppend([NotNullWhen(true)] out StoreFileAppender? appender)
    {
        appender = new InMemoryStoreFileAppender(this);
        return true;
    }
}
