using System.Collections.Generic;
using SeqCli.Forwarder.Storage;
using SeqCli.Tests.Support;
using Xunit;

namespace SeqCli.Tests.Forwarder.Storage
{
    public class LogBufferTests
    {
        const ulong DefaultBufferSize = 10 * 1024 * 1024;

        [Fact]
        public void ANewLogBufferIsEmpty()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            var contents = buffer.Peek((int)DefaultBufferSize);
            Assert.Empty(contents);
        }

        [Fact]
        public void PeekingDoesNotChangeState()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            buffer.Enqueue(new[] { Some.Bytes(140) });

            var contents = buffer.Peek((int)DefaultBufferSize);
            Assert.Single(contents);

            var remainder = buffer.Peek((int)DefaultBufferSize);
            Assert.Single(remainder);
        }

        [Fact]
        public void EnqueuedEntriesAreDequeuedFifo()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2 });
            buffer.Enqueue(new[] { a3 });

            var contents = buffer.Peek((int)DefaultBufferSize);

            Assert.Equal(3, contents.Length);
            Assert.Equal(a1, contents[0].Value);
            Assert.Equal(a2, contents[1].Value);
            Assert.Equal(a3, contents[2].Value);
        }

        [Fact]
        public void EntriesOverLimitArePurgedFifo()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), 4096);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2, a3 });

            var contents = buffer.Peek((int)DefaultBufferSize);

            Assert.Equal(2, contents.Length);
            Assert.Equal(a2, contents[0].Value);
            Assert.Equal(a3, contents[1].Value);
        }

        [Fact]
        public void SizeHintLimitsDequeuedEventCount()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2, a3 });

            var contents = buffer.Peek(300);

            Assert.Equal(2, contents.Length);
            Assert.Equal(a1, contents[0].Value);
            Assert.Equal(a2, contents[1].Value);
        }

        [Fact]
        public void AtLeastOneEventIsAlwaysDequeued()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2, a3 });

            var contents = buffer.Peek(30);

            Assert.Single(contents);
            Assert.Equal(a1, contents[0].Value);
        }

        [Fact]
        public void GivingTheLastSeenEventKeyRemovesPrecedingEvents()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2, a3 });

            var contents = buffer.Peek(420);
            Assert.Equal(3, contents.Length);

            buffer.Dequeue(contents[2].Key);

            var remaining = buffer.Peek(420);
            Assert.Empty(remaining);
        }

        [Fact]
        public void GivingTheLastSeeEventKeyLeavesSuccessiveEvents()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2, a3 });

            var contents = buffer.Peek(30);
            Assert.Single(contents);

            buffer.Enqueue(new [] { Some.Bytes(140) });

            buffer.Dequeue(contents[0].Key);

            var remaining = buffer.Peek(420);
            Assert.Equal(3, remaining.Length);
        }

        [Fact]
        public void EnumerationIsInOrder()
        {
            using var temp = TempFolder.ForCaller();
            using var buffer = new LogBuffer(temp.AllocateFilename("mdb"), DefaultBufferSize);
            byte[] a1 = Some.Bytes(140), a2 = Some.Bytes(140), a3 = Some.Bytes(140);
            buffer.Enqueue(new[] { a1, a2, a3 });

            var contents = new List<byte[]>();
            buffer.Enumerate((k, v) =>
            {
                contents.Add(v);
            });

            Assert.Equal(3, contents.Count);
            Assert.Equal(new[] { a1, a2, a3 }, contents);
        }
    }
}
