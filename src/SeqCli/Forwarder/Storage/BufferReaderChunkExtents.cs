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

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     The current read and write positions in a <see cref="BufferReaderChunk" />.
/// </summary>
readonly record struct BufferReaderChunkExtents
{
    public BufferReaderChunkExtents(long commitHead, long writeHead)
    {
        if (commitHead > writeHead)
        {
            throw new ArgumentOutOfRangeException(nameof(CommitHead), "The commit head cannot be greater than the write head");
        }

        CommitHead = commitHead;
        WriteHead = writeHead;
    }
    
    /// <summary>
    ///     The inclusive start of the range.
    /// </summary>
    public long CommitHead { get; }
    
    /// <summary>
    ///     The exclusive end of the range.
    /// </summary>
    public long WriteHead { get; }
    
    public long Unadvanced => Math.Max(0, WriteHead - CommitHead);
}
