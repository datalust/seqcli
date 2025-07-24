using System.Linq;
using SeqCli.Forwarder.Storage;
using SeqCli.Tests.Forwarder.Filesystem;
using Xunit;

namespace SeqCli.Tests.Forwarder.Storage;

public class BookmarkTests
{
    [Fact]
    public void CreateSetGet()
    {
        var directory = new InMemoryStoreDirectory();

        var bookmark = Bookmark.Open(directory);

        Assert.False(bookmark.TryGet(out var value));
        Assert.Null(value);

        Assert.True(bookmark.TrySet(new BufferPosition(42, 1)));
        Assert.True(bookmark.TryGet(out value));
        Assert.Equal(new BufferPosition(42, 1), value.Value);

        Assert.True(bookmark.TrySet(new BufferPosition(42, int.MaxValue)));
        Assert.True(bookmark.TryGet(out value));
        Assert.Equal(new BufferPosition(42, int.MaxValue), value.Value);
    }

    [Fact]
    public void OpenDeletesOldBookmarks()
    {
        var directory = new InMemoryStoreDirectory();

        directory.Create($"{1L:x16}.bookmark", new BufferPosition(3, 3478).Encode());
        directory.Create($"{3L:x16}.bookmark", new BufferPosition(42, 17).Encode());

        Assert.Equal(2, directory.Files.Count);

        var bookmark = Bookmark.Open(directory);

        Assert.Equal($"{3L:x16}.bookmark", directory.Files.Single().Key);

        Assert.True(bookmark.TryGet(out var value));
        Assert.Equal(new BufferPosition(42, 17), value);
    }

    [Fact]
    public void OpenDeletesCorruptedBookmarks()
    {
        var directory = new InMemoryStoreDirectory();

        directory.Create($"{1L:x16}.bookmark", new BufferPosition(3, 3478).Encode());

        // This bookmark is invalid
        directory.Create($"{3L:x16}.bookmark", new byte[] { 1, 2, 3 });

        var bookmark = Bookmark.Open(directory);

        Assert.Empty(directory.Files);

        Assert.True(bookmark.TrySet(new BufferPosition(42, 17)));

        Assert.Equal($"{4L:x16}.bookmark", directory.Files.Single().Key);
    }

    [Fact]
    public void OpenDeletesMisnamedBookmarks()
    {
        var directory = new InMemoryStoreDirectory();

        directory.Create($"{1L:x16}.bookmark", new BufferPosition(3, 3478).Encode());

        // This bookmark is invalid
        directory.Create($"ff{3L:x16}.bookmark", new BufferPosition(42, 17).Encode());

        var bookmark = Bookmark.Open(directory);

        Assert.Single(directory.Files);

        Assert.True(bookmark.TryGet(out var value));
        Assert.Equal(new BufferPosition(3, 3478), value);
    }
}
