using System.Linq;
using SeqCli.Forwarder.Filesystem.System;
using SeqCli.Forwarder.Storage;
using SeqCli.Tests.Forwarder.Filesystem;
using Xunit;

namespace SeqCli.Tests.Forwarder.Storage;

public class BufferTests
{
    [Fact]
    public void OpenAppendRead()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            using var writer = BufferAppender.Open(directory);
            var reader = BufferReader.Open(directory);

            Assert.Empty(directory.ListAll());

            // Append a payload
            Assert.True(writer.TryAppend("{\"id\":1}\n"u8.ToArray(), long.MaxValue));
            Assert.Single(directory.ListAll());

            // Read the payload
            Assert.False(reader.TryFillBatch(10, out _));
            Assert.True(reader.TryFillBatch(10, out var batch));
            var batchBuffer = batch.Value;
            Assert.Equal(new BufferPosition(1, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":1}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());

            // Advance the reader
            reader.AdvanceTo(batchBuffer.ReaderHead);
            Assert.False(reader.TryFillBatch(10, out batch));

            // Append another payload
            Assert.True(writer.TryAppend("{\"id\":2}\n"u8.ToArray(), long.MaxValue));
            Assert.Single(directory.ListAll());

            // Read the payload
            Assert.True(reader.TryFillBatch(10, out batch));
            batchBuffer = batch.Value;
            Assert.Equal(new BufferPosition(1, 18), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":2}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());

            // Advance the reader
            reader.AdvanceTo(batchBuffer.ReaderHead);
            Assert.False(reader.TryFillBatch(10, out batch));
        });
    }

    [Fact]
    public void ReadWaitsUntilCompleteDataOnLastChunk()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            var chunk = directory.Create(new ChunkName(1).ToString(), "{\"id\":1"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.False(reader.TryFillBatch(512, out _));

            chunk.Append("}"u8.ToArray());

            Assert.False(reader.TryFillBatch(512, out _));

            chunk.Append("\n"u8.ToArray());

            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(1, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":1}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());
        });
    }

    [Fact]
    public void ReadDiscardsPreviouslyReadChunks()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(1).ToString(), "{\"id\":1}\n"u8.ToArray());
            directory.Create(new ChunkName(2).ToString(), "{\"id\":2}\n"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;
            Assert.Equal(new BufferPosition(2, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":1}\n{\"id\":2}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());

            Assert.Equal(2, directory.ListAll().Count());

            reader.AdvanceTo(batchBuffer.ReaderHead);

            Assert.Single(directory.ListAll());

            directory.Create(new ChunkName(1).ToString(), "{\"id\":1}\n"u8.ToArray());
            directory.Create(new ChunkName(3).ToString(), "{\"id\":3}\n"u8.ToArray());

            Assert.Equal(3, directory.ListAll().Count());

            Assert.False(reader.TryFillBatch(512, out _));
            Assert.True(reader.TryFillBatch(512, out batch));
            batchBuffer = batch.Value;
            Assert.Equal(new BufferPosition(3, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":3}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());

            reader.AdvanceTo(batchBuffer.ReaderHead);

            Assert.Single(directory.ListAll());
        });
    }

    [Fact]
    public void AdvancingToNonexistentLowerPositionRereadsAllChunks()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(71).ToString(), "{\"id\":1}\n"u8.ToArray());
            directory.Create(new ChunkName(72).ToString(), "{\"id\":2}\n"u8.ToArray());

            var reader = BufferReader.Open(directory);

            reader.AdvanceTo(new BufferPosition(60, 0));

            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;
            Assert.Equal(new BufferPosition(72, 9), batchBuffer.ReaderHead);
        });
    }
    
    [Fact]
    public void AdvancingToNonexistentHigherPositionDiscardsAllChunks()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(71).ToString(), "{\"id\":1}\n"u8.ToArray());
            directory.Create(new ChunkName(72).ToString(), "{\"id\":2}\n"u8.ToArray());

            var reader = BufferReader.Open(directory);

            reader.AdvanceTo(new BufferPosition(80, 0));

            Assert.False(reader.TryFillBatch(512, out _));
        });
    }

    [Fact]
    public void ReadDiscardsOversizePayloads()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            using var writer = BufferAppender.Open(directory);

            Assert.True(writer.TryAppend("{\"id\":1}\n"u8.ToArray(), long.MaxValue));

            var reader = BufferReader.Open(directory);

            Assert.False(reader.TryFillBatch(5, out _));
            Assert.False(reader.TryFillBatch(512, out _));
        });
    }

    [Fact]
    public void ReadDoesNotDiscardAcrossFiles()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(1).ToString(), "{\"id\":1}"u8.ToArray());
            directory.Create(new ChunkName(2).ToString(), "{\"id\":2}\n"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(2, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":2}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());
        });
    }

    [Fact]
    public void ReadStopsDiscardingOnExternalDelete()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(1).ToString(), "{\"id\":1}"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.False(reader.TryFillBatch(5, out _));

            // Deleting the file here will cause our discarding chunk to change
            Assert.True(directory.TryDelete(new ChunkName(1).ToString()));
            directory.Create(new ChunkName(2).ToString(), "{\"id\":2}\n"u8.ToArray());

            Assert.False(reader.TryFillBatch(512, out _));
            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(2, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":2}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());
        });
    }
    
    [Fact]
    public void ReadStopsDiscardingOnExternalTruncate()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(1).ToString(), "{\"id\":1}"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.False(reader.TryFillBatch(5, out _));

            // Changing the length of the file here will cause our discarding chunk to change
            directory.ReplaceContents(new ChunkName(1).ToString(), "{}"u8.ToArray());
            directory.Create(new ChunkName(2).ToString(), "{\"id\":2}\n"u8.ToArray());

            Assert.False(reader.TryFillBatch(512, out _));
            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(2, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":2}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());
        });
    }

    [Fact]
    public void ReadStopsDiscardingOnExternalCreate()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(1).ToString(), "{\"id\":1}"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.False(reader.TryFillBatch(5, out _));

            // Creating a new file here will stop discarding
            directory.Create(new ChunkName(2).ToString(), "{\"id\":2}\n"u8.ToArray());

            Assert.False(reader.TryFillBatch(512, out _));
            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(2, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":2}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());
        });
    }

    [Fact]
    public void AppendRolloverOnWrite()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            using var writer = BufferAppender.Open(directory);
            var reader = BufferReader.Open(directory);

            Assert.Empty(directory.ListAll());

            Assert.True(writer.TryAppend("{\"id\":1}\n"u8.ToArray(), 17));
            Assert.True(writer.TryAppend("{\"id\":2}\n"u8.ToArray(), 17));

            Assert.Single(directory.ListAll());

            Assert.True(writer.TryAppend("{\"id\":3}\n"u8.ToArray(), 17));

            Assert.Equal(2, directory.ListAll().Count());

            Assert.False(reader.TryFillBatch(512, out _));
            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(2, 9), batchBuffer.ReaderHead);
            Assert.Equal("{\"id\":1}\n{\"id\":2}\n{\"id\":3}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());

            reader.AdvanceTo(batchBuffer.ReaderHead);

            Assert.Single(directory.ListAll());
        });
    }

    [Fact]
    public void ExistingFilesAreReadonly()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(0).ToString());

            using var writer = BufferAppender.Open(directory);
            var reader = BufferReader.Open(directory);

            Assert.Single(directory.ListAll());

            Assert.True(writer.TryAppend("{\"id\":1}\n"u8.ToArray(), long.MaxValue));

            Assert.Equal(2, directory.ListAll().Count());

            Assert.False(reader.TryFillBatch(512, out _));
            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal(new BufferPosition(1, 9), batchBuffer.ReaderHead);
        });
    }

    [Fact]
    public void OpenReadAcrossChunks()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            directory.Create(new ChunkName(0).ToString(), "{\"id\":1}\n"u8.ToArray());
            directory.Create(new ChunkName(1).ToString(), "{\"id\":2}\n"u8.ToArray());
            directory.Create(new ChunkName(2).ToString(), "{\"id\":3}\n"u8.ToArray());

            var reader = BufferReader.Open(directory);

            Assert.Equal(3, directory.ListAll().Count());

            Assert.True(reader.TryFillBatch(512, out var batch));
            var batchBuffer = batch.Value;

            Assert.Equal("{\"id\":1}\n{\"id\":2}\n{\"id\":3}\n"u8.ToArray(), batchBuffer.AsSpan().ToArray());

            reader.AdvanceTo(batchBuffer.ReaderHead);

            Assert.Single(directory.ListAll());
        });
    }

    [Fact]
    public void MaxChunksOnAppender()
    {
        TestFilesystem.PermuteFilesystem(directory =>
        {
            using var appender = BufferAppender.Open(directory);

            for (var i = 0; i < 10; i++) Assert.True(appender.TryAppend("{\"id\":1}\n"u8.ToArray(), 5, 3));

            var files = directory.ListAll().Select(f => f.Name).ToList();
            files.Sort();

            Assert.Equal([
                "0000000000000008.clef",
                "0000000000000009.clef",
                "000000000000000a.clef"
            ], files);
        });
    }
}
