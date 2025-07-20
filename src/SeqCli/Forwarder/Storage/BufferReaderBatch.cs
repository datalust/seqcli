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

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     A contiguous batch of records pulled from a reader.
/// </summary>
readonly record struct BufferReaderBatch
{
    readonly int _length;

    readonly ArrayPool<byte>? _pool;
    readonly byte[] _storage;

    public BufferReaderBatch(BufferReaderHead readerHead, ArrayPool<byte>? pool, byte[] storage, int length)
    {
        ReaderHead = readerHead;

        _pool = pool;
        _storage = storage;
        _length = length;
    }

    public BufferReaderHead ReaderHead { get; }

    public ReadOnlySpan<byte> AsSpan()
    {
        return _storage.AsSpan()[.._length];
    }

    public ArraySegment<byte> AsArraySegment()
    {
        return _storage[.._length];
    }

    public void Return()
    {
        _pool?.Return(_storage);
    }
}
