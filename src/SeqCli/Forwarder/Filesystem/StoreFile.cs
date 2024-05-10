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
using System.Diagnostics.CodeAnalysis;

namespace SeqCli.Forwarder.Filesystem;

abstract class StoreFile
{
    /// <summary>
    ///     Get the length of this file.
    /// </summary>
    /// <returns>
    ///     True if the length was read, false if the file is invalid.
    /// </returns>
    public abstract bool TryGetLength([NotNullWhen(true)] out long? length);

    /// <summary>
    ///     Read the complete contents of this file.
    /// </summary>
    /// <returns>
    ///     The number of bytes copied.
    /// </returns>
    public virtual long CopyContentsTo(Span<byte> buffer)
    {
        if (!TryGetLength(out var length)) throw new Exception("Failed to get the length of a file.");

        if (!TryOpenRead(length.Value, out var opened)) throw new Exception("Failed to open a reader to a file.");

        using var reader = opened;
        return reader.CopyTo(buffer);
    }

    /// <summary>
    ///     Try open a reader to the file.
    /// </summary>
    /// <returns>
    ///     True if the file was opened for reading, false if the file is invalid.
    /// </returns>
    public abstract bool TryOpenRead(long length, [NotNullWhen(true)] out StoreFileReader? reader);

    /// <summary>
    ///     Open a writer to the file.
    /// </summary>
    /// <remarks>
    ///     Only a single writer to a file should be open at a given time.
    ///     Overlapping writers may result in data corruption.
    /// </remarks>
    /// <returns>
    ///     True if the file was opened for writing, false if the file is invalid.
    /// </returns>
    public abstract bool TryOpenAppend([NotNullWhen(true)] out StoreFileAppender? appender);
}
