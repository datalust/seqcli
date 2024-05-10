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
using System.Buffers.Binary;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     The in-memory value of a bookmark.
/// </summary>
readonly record struct BookmarkValue(ulong Id, long CommitHead)
{
    public void EncodeTo(Span<byte> bookmark)
    {
        if (bookmark.Length != 16) throw new Exception($"Bookmark values must be 16 bytes (got {bookmark.Length}).");

        BinaryPrimitives.WriteUInt64LittleEndian(bookmark, Id);
        BinaryPrimitives.WriteInt64LittleEndian(bookmark[8..], CommitHead);
    }

    public byte[] Encode()
    {
        var buffer = new byte[16];
        EncodeTo(buffer);

        return buffer;
    }

    public static BookmarkValue Decode(Span<byte> bookmark)
    {
        if (bookmark.Length != 16) throw new Exception($"Bookmark values must be 16 bytes (got {bookmark.Length}).");

        var id = BinaryPrimitives.ReadUInt64LittleEndian(bookmark);
        var commitHead = BinaryPrimitives.ReadInt64LittleEndian(bookmark[8..]);

        return new BookmarkValue
        {
            Id = id,
            CommitHead = commitHead
        };
    }
}
