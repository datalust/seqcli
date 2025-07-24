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
using System.IO;
using System.Linq;
using System.Threading;
using SeqCli.Forwarder.Filesystem;
using Path = System.IO.Path;

namespace SeqCli.Forwarder.Storage;

/// <summary>
///     A durable bookmark of progress processing buffers.
/// </summary>
sealed class Bookmark
{
    readonly StoreDirectory _storeDirectory;

    readonly Lock _sync = new();
    BookmarkName _name;
    BufferPosition? _value;

    Bookmark(StoreDirectory storeDirectory, BookmarkName name, BufferPosition? value)
    {
        _storeDirectory = storeDirectory;
        _name = name;
        _value = value;
    }

    public static Bookmark Open(StoreDirectory storeDirectory)
    {
        var (name, value) = Read(storeDirectory);

        return new Bookmark(storeDirectory, name, value);
    }

    public bool TryGet([NotNullWhen(true)] out BufferPosition? bookmark)
    {
        lock (_sync)
        {
            if (_value != null)
            {
                bookmark = _value.Value;
                return true;
            }

            bookmark = null;
            return false;
        }
    }

    public bool TrySet(BufferPosition value, bool sync = true)
    {
        lock (_sync)
        {
            _value = value;
        }

        try
        {
            Write(_storeDirectory, _name, value, sync);
            return true;
        }
        catch (IOException)
        {
            _name = new BookmarkName(_name.Id + 1);
            return false;
        }
    }

    static void Write(StoreDirectory storeDirectory, BookmarkName name, BufferPosition value, bool fsync)
    {
        unsafe
        {
            Span<byte> bookmark = stackalloc byte[16];
            value.EncodeTo(bookmark);

            storeDirectory.ReplaceContents(name.ToString(), bookmark, fsync);
        }
    }

    static (BookmarkName, BufferPosition?) Read(StoreDirectory storeDirectory)
    {
        // NOTE: This method shouldn't throw
        var bookmarks = new List<(string, BookmarkName, StoreFile)>();

        foreach (var (candidateFileName, candidateFile) in storeDirectory
                     .List(candidateFileName => Path.GetExtension(candidateFileName) is ".bookmark"))
            if (BookmarkName.TryParse(candidateFileName, out var parsedBookmarkName))
                bookmarks.Add((candidateFileName, parsedBookmarkName.Value, candidateFile));
            else
                // The `.bookmark` file uses an unrecognized naming convention
                storeDirectory.TryDelete(candidateFileName);

        switch (bookmarks.Count)
        {
            // There aren't any bookmarks; return a default one
            case 0:
                return (new BookmarkName(0), null);
            // There are old bookmark values floating around; try delete them again
            case > 1:
            {
                bookmarks.Sort((a, b) => a.Item2.Id.CompareTo(b.Item2.Id));

                foreach (var (toDelete, _, _) in bookmarks.Take(bookmarks.Count - 1))
                    storeDirectory.TryDelete(toDelete);
                break;
            }
        }

        var (fileName, bookmarkName, file) = bookmarks[^1];

        try
        {
            unsafe
            {
                Span<byte> bookmark = stackalloc byte[16];
                if (file.CopyContentsTo(bookmark) != 16) throw new Exception("The bookmark is corrupted.");

                return (bookmarkName, BufferPosition.Decode(bookmark));
            }
        }
        catch
        {
            storeDirectory.TryDelete(fileName);

            return (new BookmarkName(bookmarkName.Id + 1), new BufferPosition());
        }
    }
}
