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
using SeqCli.Forwarder.Filesystem;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     An active chunk in a <see cref="BufferReader" />.
/// </summary>
class BufferReaderChunk : IDisposable
{
    public BufferReaderChunk(ChunkName name, StoreFile chunk)
    {
        Name = name;
        Chunk = chunk;
    }

    public ChunkName Name { get; }
    public StoreFile Chunk { get; }

    (long, StoreFileReader)? _reader;

    public void Dispose()
    {
        _reader?.Item2.Dispose();
    }

    public bool TryCopyTo(Span<byte> buffer, BufferReaderChunkHead head, int fill)
    {
        var readEnd = head.CommitHead + fill;

        if (_reader != null)
            if (_reader.Value.Item1 < readEnd)
            {
                var toDispose = _reader.Value.Item2;
                _reader = null;

                toDispose.Dispose();
            }

        if (_reader == null)
        {
            if (!Chunk.TryOpenRead(head.WriteHead, out var reader)) return false;

            _reader = (head.WriteHead, reader);
        }

        _reader.Value.Item2.CopyTo(buffer, head.CommitHead, fill);

        return true;
    }
}
