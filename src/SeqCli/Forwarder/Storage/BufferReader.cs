// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     The read-side of a buffer.
/// </summary>
sealed class BufferReader
{
    readonly StoreDirectory _storeDirectory;
    BufferPosition? _discardingHead;
    BufferPosition _readHead;
    List<BufferReaderChunk> _sortedChunks;

    BufferReader(StoreDirectory storeDirectory)
    {
        _sortedChunks = [];
        _storeDirectory = storeDirectory;
        _discardingHead = null;
        _readHead = new(0, 0);
    }

    public static BufferReader Open(StoreDirectory storeDirectory)
    {
        var reader = new BufferReader(storeDirectory);
        reader.ReadChunks();

        return reader;
    }

    /// <summary>
    /// Try fill a batch from the underlying file set.
    ///
    /// This method does not throw.
    ///
    /// This method is expected to be called in a loop to continue filling and processing batches as they're written.
    ///
    /// Once the batch is processed, call <see cref="AdvanceTo"/> to advance the reader past it.
    /// </summary>
    /// <param name="maxSize">The maximum size in bytes of a batch to read. If a single value between newlines is larger
    /// than this size then it will be discarded rather than read.</param>
    /// <param name="batch">The newline-delimited batch of values read.</param>
    /// <returns>True if a batch was filled. If this method returns false, then it means there is either no new
    /// data to read, some oversize data was discarded, or an IO error was encountered.</returns>
    public bool TryFillBatch(int maxSize, [NotNullWhen(true)] out BufferReaderBatch? batch)
    {
        /*
        This is where the meat of the buffer reader lives. Reading batches runs in two broad steps:

        1. If a previous batch overflowed the buffer then we're in "discard mode".
           Scan through the offending chunk until a newline delimiter is found.
        2. After discarding, attempt to fill a buffer with as much data as possible
           from the underlying chunks.
        */

        if (_discardingHead != null)
        {
            var discardingRentedArray = ArrayPool<byte>.Shared.Rent(maxSize);

            // NOTE: We don't use `maxSize` here, because we're discarding these bytes
            // so it doesn't matter what size the target array is
            var discardingBatchBuffer = discardingRentedArray.AsSpan();

            while (_discardingHead != null)
            {
                var chunk = _sortedChunks[0];

                // If the chunk has changed (it may have been deleted externally)
                // then stop discarding
                if (chunk.Name.Id != _discardingHead.Value.ChunkId)
                {
                    _discardingHead = null;

                    ArrayPool<byte>.Shared.Return(discardingRentedArray);
                    break;
                }

                var chunkHead = Extents(chunk);

                // Attempt to fill the buffer with data from the underlying chunk
                if (!TryFillChunk(chunk,
                        chunkHead with { CommitHead = _discardingHead.Value.Offset },
                        discardingBatchBuffer,
                        out var fill))
                {
                    // If attempting to read from the chunk fails then remove it and carry on
                    // This is also done below in the regular read-loop if reading fails
                    _sortedChunks.RemoveAt(0);
                    _discardingHead = null;

                    ArrayPool<byte>.Shared.Return(discardingRentedArray);
                    break;
                }

                // Scan forwards for the next newline
                var firstNewlineIndex = discardingBatchBuffer[..fill.Value].IndexOf((byte)'\n');

                // If a newline was found then advance the reader to it and stop discarding
                if (firstNewlineIndex >= 0) fill = firstNewlineIndex + 1;

                _discardingHead = _discardingHead.Value with
                {
                    Offset = _discardingHead.Value.Offset + fill.Value
                };
                _readHead = _discardingHead.Value;

                var isChunkFinished = _discardingHead.Value.Offset == chunkHead.WriteHead;

                // If the chunk is finished or a newline is found then stop discarding
                if (firstNewlineIndex >= 0 || (isChunkFinished && _sortedChunks.Count > 1))
                {
                    _discardingHead = null;

                    ArrayPool<byte>.Shared.Return(discardingRentedArray);
                    break;
                }

                // If there's more data in the chunk to read then loop back through
                if (!isChunkFinished) continue;

                // If the chunk is finished but a newline wasn't found then refresh
                // our set of chunks and loop back through
                ReadChunks();

                ArrayPool<byte>.Shared.Return(discardingRentedArray);
                batch = null;
                return false;
            }
        }

        // Fill a buffer with newline-delimited values

        var rentedArray = ArrayPool<byte>.Shared.Rent(maxSize);
        var batchBuffer = rentedArray.AsSpan()[..maxSize];
        var batchLength = 0;

        BufferPosition? batchHead = null;
        var chunkIndex = 0;

        // Try fill the buffer with as much data as possible
        // by walking over all chunks
        while (chunkIndex < _sortedChunks.Count)
        {
            var chunk = _sortedChunks[chunkIndex];
            var chunkHead = Extents(chunk);

            if (!TryFillChunk(chunk, chunkHead, batchBuffer[batchLength..], out var fill))
            {
                // If we can't read from this chunk anymore then remove it and continue
                _sortedChunks.RemoveAt(chunkIndex);
                continue;
            }

            var isBufferFull = batchLength + fill == maxSize;
            var isChunkFinished = fill == chunkHead.WriteHead;

            // If either the buffer has been filled or we've reached the end of a chunk
            // then scan to the last newline
            if (isBufferFull || isChunkFinished)
            {
                // If the chunk is finished then we expect this to immediately find a trailing newline
                // NOTE: `Span.LastIndexOf` and similar methods are vectorized
                var lastNewlineIndex = batchBuffer[batchLength..(batchLength + fill.Value)].LastIndexOf((byte)'\n');
                if (lastNewlineIndex == -1)
                {
                    // If this isn't the last chunk then discard the trailing data and move on
                    if (isChunkFinished && chunkIndex < _sortedChunks.Count)
                    {
                        chunkIndex += 1;
                        continue;
                    }

                    // If this is the first chunk then we've hit an oversize payload
                    if (chunkIndex == 0)
                    {
                        _discardingHead = new BufferPosition(chunk.Name.Id, chunkHead.CommitHead + fill.Value);

                        // Ensures we don't attempt to yield the data we've read
                        batchHead = null;
                    }

                    // If the chunk isn't finished then the buffer is full
                    break;
                }

                fill = lastNewlineIndex + 1;
            }

            batchLength += fill.Value;
            batchHead = new BufferPosition(chunk.Name.Id, chunkHead.CommitHead + fill.Value);

            chunkIndex += 1;
        }

        // If the batch is empty (because there are no chunks or there's no new data)
        // then refresh the set of chunks and return
        if (batchHead == null || batchLength == 0)
        {
            ReadChunks();

            ArrayPool<byte>.Shared.Return(rentedArray);
            batch = null;
            return false;
        }

        // If the batch is non-empty then return it
        batch = new BufferReaderBatch(batchHead.Value, ArrayPool<byte>.Shared, rentedArray, batchLength);
        return true;
    }

    /// <summary>
    /// Advance the reader over a previously read batch.
    ///
    /// This method does not throw.
    /// </summary>
    /// <param name="newReaderHead">The new head to resume reading from.</param>
    public void AdvanceTo(BufferPosition newReaderHead)
    {
        var removeLength = 0;

        foreach (var chunk in _sortedChunks)
        {
            // A portion of the chunk is being skipped
            if (chunk.Name.Id == newReaderHead.ChunkId) break;

            // The remainder of the chunk is being skipped
            if (chunk.Name.Id < newReaderHead.ChunkId)
            {
                _storeDirectory.TryDelete(chunk.Name.ToString());
            }
            else
            {
                // We might end up here if a chunk in the middle of the range was
                // deleted from disk, while a saved bookmark references that chunk.
                break;
            }

            removeLength += 1;
        }

        _readHead = newReaderHead;
        _sortedChunks.RemoveRange(0, removeLength);
    }

    BufferReaderChunkExtents Extents(BufferReaderChunk chunk)
    {
        if (chunk.Name.Id == _readHead.ChunkId)
            return chunk.Chunk.TryGetLength(out var writeHead)
                ? new BufferReaderChunkExtents(Math.Min(_readHead.Offset, writeHead.Value), writeHead.Value)
                : new BufferReaderChunkExtents(_readHead.Offset, _readHead.Offset);

        chunk.Chunk.TryGetLength(out var length);
        return new BufferReaderChunkExtents(0, length ?? 0);
    }

    void ReadChunks()
    {
        List<BufferReaderChunk> chunks = [];

        foreach (var (fileName, file) in _storeDirectory
                     .List(candidateName => Path.GetExtension(candidateName) is ".clef"))
        {
            if (!ChunkName.TryParse(fileName, out var parsedChunkName)) continue;

            if (parsedChunkName.Value.Id >= _readHead.ChunkId)
                chunks.Add(new BufferReaderChunk(parsedChunkName.Value, file));
            else
                // If the chunk is before the one we're expecting to read then delete it; we've already processed it
                _storeDirectory.TryDelete(fileName);
        }

        chunks.Sort((a, b) => a.Name.Id.CompareTo(b.Name.Id));

        var toDispose = _sortedChunks;
        _sortedChunks = chunks;

        foreach (var chunk in toDispose)
            try
            {
                chunk.Dispose();
            }
            catch
            {
                // Ignored
            }
    }

    static bool TryFillChunk(BufferReaderChunk chunk, BufferReaderChunkExtents chunkExtents, Span<byte> buffer,
        [NotNullWhen(true)] out int? filled)
    {
        var remaining = buffer.Length;
        var fill = (int)Math.Min(remaining, chunkExtents.Unadvanced);

        try
        {
            if (!chunk.TryCopyTo(buffer, chunkExtents, fill))
            {
                filled = null;
                return false;
            }

            filled = fill;
            return true;
        }
        catch (IOException)
        {
            filled = null;
            return false;
        }
    }
}
