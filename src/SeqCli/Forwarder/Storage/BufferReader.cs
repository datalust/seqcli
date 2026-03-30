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
        
        var chunkIndex = 0;

        if (_discardingHead != null)
        {
            // We're discarding an oversize payload
            var discardingRentedArray = ArrayPool<byte>.Shared.Rent(maxSize);

            // NOTE: We don't use `maxSize` here, because we're discarding these bytes
            // so it doesn't matter what size the target array is
            var discardingBatchBuffer = discardingRentedArray.AsSpan();

            while (_discardingHead != null && chunkIndex < _sortedChunks.Count)
            {
                var chunk = _sortedChunks[chunkIndex];

                // If the first chunk has changed (it may have been deleted externally)
                // then stop discarding
                if (chunk.Name.Id != _discardingHead.Value.ChunkId)
                {
                    _discardingHead = null;

                    break;
                }

                // Try read to the end of the chunk
                //
                // If reading the chunk length fails then advance over it
                if (!chunk.File.TryGetLength(out var length))
                {
                    chunkIndex += 1;

                    continue;
                }

                var chunkHead = new BufferReaderChunkExtents(Math.Min(length.Value, _discardingHead.Value.Offset), length.Value);

                // Attempt to fill the buffer with data from the underlying chunk
                //
                // If reading from the chunk fails then advance over it
                if (!TryFillChunk(chunk,
                        chunkHead,
                        discardingBatchBuffer,
                        out var filled))
                {
                    chunkIndex += 1;

                    continue;
                }

                // Scan forwards for the next newline
                var firstNewlineIndex = discardingBatchBuffer[..filled.Value].IndexOf((byte)'\n');
                if (firstNewlineIndex >= 0) filled = firstNewlineIndex + 1;

                _discardingHead = _discardingHead.Value with
                {
                    Offset = _discardingHead.Value.Offset + filled.Value
                };
                _readHead = _discardingHead.Value;

                // If a newline was found then advance the reader to it and stop discarding
                if (firstNewlineIndex >= 0)
                {
                    _discardingHead = null;

                    break;
                }
                
                var isChunkFinished = chunkHead.CommitHead + filled == chunkHead.WriteHead;
                
                // If we've discarded to the end of the chunk then update our state from the disk and return
                //
                // The next time we attempt to fill a chunk we'll resume from this point.
                if (isChunkFinished)
                {
                    // If there's no way new data can arrive to complete this event then advance over it.
                    // If the chunk is the last one then it's considered actively writable, and so we
                    // presume we're seeing a torn write here.
                    //
                    // A future sync from the files on disk will delete it.
                    if (_sortedChunks.Count > 1)
                    {
                        _discardingHead = null;

                        break;
                    }
                    
                    // There's only a single chunk, update our state from the disk in case the writer
                    // has moved on to another chunk and return. We may end up coming back later and
                    // reading more to discard.
                    ReadChunks();

                    ArrayPool<byte>.Shared.Return(discardingRentedArray);
                    batch = null;
                    return false;
                }
            }
            
            ReadChunks();

            ArrayPool<byte>.Shared.Return(discardingRentedArray);
        }

        // Fill a buffer with newline-delimited values

        var rentedArray = ArrayPool<byte>.Shared.Rent(maxSize);
        var batchBuffer = rentedArray.AsSpan()[..maxSize];
        var batchLength = 0;

        BufferPosition? batchHead = null;

        // Try fill the buffer with as much data as possible
        // by walking over all chunks
        while (chunkIndex < _sortedChunks.Count)
        {
            var chunk = _sortedChunks[chunkIndex];
            
            BufferReaderChunkExtents chunkHead;
            if (chunk.Name.Id == _readHead.ChunkId)
            {
                // The chunk is the one we're currently reading; resume from where we left off
                // If the file was truncated externally then we'll treat it as complete
                chunkHead = chunk.File.TryGetLength(out var length)
                    ? new BufferReaderChunkExtents(Math.Min(_readHead.Offset, length.Value), length.Value)
                    : new BufferReaderChunkExtents(_readHead.Offset, _readHead.Offset);
            }
            else
            {
                // The chunk is not the one we've been reading; start from the beginning
                chunk.File.TryGetLength(out var length);
                chunkHead = new BufferReaderChunkExtents(0, length ?? 0);
            }

            if (!TryFillChunk(chunk, chunkHead, batchBuffer[batchLength..], out var filled))
            {
                // If we can't read from this chunk anymore then step over it
                chunkIndex += 1;
                continue;
            }

            var isBufferFull = batchLength + filled == maxSize;
            var isChunkFinished = chunkHead.CommitHead + filled == chunkHead.WriteHead;

            // If either the buffer has been filled or we've reached the end of the chunk
            // then scan backwards to the last newline delimiter
            if (isBufferFull || isChunkFinished)
            {
                // If the chunk is valid and finished then we expect this to immediately find a trailing newline
                // NOTE: `Span.LastIndexOf` and similar methods are vectorized
                var lastNewlineIndex = batchBuffer[batchLength..(batchLength + filled.Value)].LastIndexOf((byte)'\n');
                if (lastNewlineIndex == -1)
                {
                    // The data we wrote didn't contain any newline delimiters
                    
                    // If there's no way new data can arrive to complete this event then advance over it.
                    // If the chunk is the last one then it's considered actively writable, and so we
                    // presume we're seeing a torn write here.
                    //
                    // A subsequent attempt to fill will overwrite the incomplete data in it, and
                    // a future sync from the files on disk will delete it
                    if (isChunkFinished && chunkIndex < _sortedChunks.Count)
                    {
                        chunkIndex += 1;
                        continue;
                    }

                    // If we're looking at the first chunk then start discarding
                    //
                    // We'll hit this point if we happen to start from an oversized payload, or if our last attempt
                    // to fill a batch advanced up to an oversize event
                    if (chunkIndex == 0)
                    {
                        _discardingHead = new BufferPosition(chunk.Name.Id, chunkHead.CommitHead + filled.Value);

                        // Ensures we don't attempt to yield the data we've read
                        batchHead = null;
                    }

                    // If the chunk isn't finished then the buffer is full
                    break;
                }

                // Only consider the read data up to the last newline
                filled = lastNewlineIndex + 1;
            }

            batchLength += filled.Value;
            batchHead = new BufferPosition(chunk.Name.Id, chunkHead.CommitHead + filled.Value);

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
                chunk.Dispose();
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

    /// <summary>
    /// Read the current state of the store from files on disk.
    /// </summary>
    /// <remarks>
    /// This method will delete any files it finds before the current read head.
    /// </remarks>
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
