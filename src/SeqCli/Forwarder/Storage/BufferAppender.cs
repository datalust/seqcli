// Copyright Datalust Pty Ltd
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     The write-side of a buffer.
/// </summary>
public sealed class BufferAppender : IDisposable
{
    readonly StoreDirectory _storeDirectory;
    BufferAppenderChunk? _currentChunk;

    BufferAppender(StoreDirectory storeDirectory)
    {
        _storeDirectory = storeDirectory;
        _currentChunk = null;
    }

    public void Dispose()
    {
        _currentChunk?.Dispose();
    }

    public static BufferAppender Open(StoreDirectory storeDirectory)
    {
        return new BufferAppender(storeDirectory);
    }

    /// <summary>
    /// Try write a batch.
    ///
    /// This method does not throw.
    ///
    /// This method will write the batch into the currently active chunk file unless:
    ///
    /// 1. The length of the current chunk is greater than <paramref name="targetChunkLength"/> or,
    /// 2. There is no current chunk, because no writes have been made, or it encountered an IO error previously.
    ///
    /// If either of these cases is true, then the write will be made to a new chunk file.
    /// </summary>
    /// <param name="batch">The newline-delimited data to write. A batch may contain multiple values separated by
    /// newlines, but must end on a newline.</param>
    /// <param name="targetChunkLength">The file size to roll on. A single batched write may cause the currently
    /// active chunk to exceed this size, but a subsequent write will roll over to a new file.</param>
    /// <param name="maxChunks">The maximum number of chunk files to keep before starting to delete them. This
    /// is an optional parameter to use in cases where the reader isn't keeping up with the writer.</param>
    /// <param name="sync">Whether to explicitly flush the write to disk.</param>
    /// <returns>True if the write fully succeeded. If this method returns false, it is safe to retry the write,
    /// but it may result in duplicate data in the case of partial success.</returns>
    public bool TryAppend(Span<byte> batch, long targetChunkLength, int? maxChunks = null, bool sync = true)
    {
        if (batch.Length == 0) return true;

        if (batch[^1] != (byte)'\n') throw new Exception("Batches must end with a newline character (\\n)");

        if (_currentChunk != null)
            // Only use the existing chunk if it's writable and shouldn't be rolled over
            if (_currentChunk.WriteHead > targetChunkLength)
            {
                // Run a sync before moving to a new file, just to make sure any
                // buffered data makes its way to disk
                _currentChunk.Appender.Sync();

                _currentChunk.Dispose();
                _currentChunk = null;
            }

        // If there's no suitable candidate chunk then create a new one
        if (_currentChunk == null)
        {
            var nextChunkId = ReadChunks(_storeDirectory, maxChunks);

            var chunkName = new ChunkName(nextChunkId);

            var chunkFile = _storeDirectory.Create(chunkName.ToString());

            if (chunkFile.TryOpenAppend(out var opened))
                _currentChunk = new BufferAppenderChunk(opened);
            else
                return false;
        }

        try
        {
            _currentChunk.Appender.Append(batch);
            _currentChunk.Appender.Commit();

            if (sync) _currentChunk.Appender.Sync();

            _currentChunk.WriteHead += batch.Length;

            return true;
        }
        catch (IOException)
        {
            // Don't try an explicit sync here, because the file already failed to perform IO

            _currentChunk.Dispose();
            _currentChunk = null;

            return false;
        }
    }

    static ulong ReadChunks(StoreDirectory storeDirectory, int? maxChunks)
    {
        ulong nextChunkId = 0;

        List<ChunkName>? chunks = null;
        foreach (var (fileName, _) in storeDirectory.List(candidateName =>
                     Path.GetExtension(candidateName) is ".clef"))
        {
            if (!ChunkName.TryParse(fileName, out var parsedChunkName)) continue;

            nextChunkId = Math.Max(nextChunkId, parsedChunkName.Value.Id);

            if (maxChunks == null) continue;

            chunks ??= [];
            chunks.Add(parsedChunkName.Value);
        }

        // Apply retention on the number of chunk files if the reader isn't keeping up
        if (chunks != null)
        {
            ApplyPreWriteRetention(storeDirectory, maxChunks!.Value, chunks);
        }

        return nextChunkId + 1;
    }

    static void ApplyPreWriteRetention(StoreDirectory storeDirectory, int maxChunks, List<ChunkName> unsortedChunks)
    {
        // We're going to create a new buffer file, so leave room for it if a max is specified
        maxChunks = Math.Max(0, maxChunks - 1);

        unsortedChunks.Sort((a, b) => a.Id.CompareTo(b.Id));
        var sortedChunks = unsortedChunks;

        if (sortedChunks.Count > maxChunks)
            foreach (var delete in sortedChunks.Take(sortedChunks.Count - maxChunks))
                // This call may fail if a reader is actively holding this file open
                // In these cases we let the writer proceed instead of blocking
                storeDirectory.TryDelete(delete.ToString());
    }
}
