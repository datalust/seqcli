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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SeqCli.Forwarder.Filesystem;

/// <summary>
///     A container of <see cref="StoreFile" />s and their names.
/// </summary>
abstract class StoreDirectory
{
    /// <summary>
    ///     Create a new file with the given name, linking it into the filesystem.
    /// </summary>
    public abstract StoreFile Create(string name);

    public virtual (string, StoreFile) CreateTemporary()
    {
        var tmpName = $"rc{Guid.NewGuid():N}.tmp";
        return (tmpName, Create(tmpName));
    }

    /// <summary>
    ///     Delete a file with the given name, returning whether the file was deleted.
    /// </summary>
    public abstract bool TryDelete(string name);

    /// <summary>
    ///     Atomically replace the contents of one file with another, creating it if it doesn't exist and deleting the other.
    /// </summary>
    public abstract StoreFile Replace(string toReplace, string replaceWith);

    /// <summary>
    ///     Atomically replace the contents of a file.
    /// </summary>
    public virtual StoreFile ReplaceContents(string name, Span<byte> contents, bool sync = true)
    {
        var (tmpName, tmpFile) = CreateTemporary();

        try
        {
            if (!tmpFile.TryOpenAppend(out var opened))
                throw new Exception("Failed to write to a temporary file that was just created.");

            using var writer = opened;
            writer.Append(contents);
            writer.Commit();

            if (sync) writer.Sync();
        }
        catch
        {
            TryDelete(tmpName);
            throw;
        }

        return Replace(name, tmpName);
    }

    /// <summary>
    ///     List all files in unspecified order.
    /// </summary>
    public abstract IEnumerable<(string Name, StoreFile File)> List(Func<string, bool> predicate);

    /// <summary>
    ///     Try get a file by name.
    /// </summary>
    public virtual bool TryGet(string name, [NotNullWhen(true)] out StoreFile? file)
    {
        file = List(n => n == name)
            .Select(p => p.File)
            .FirstOrDefault();

        return file != null;
    }
}
