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
using System.IO;
using System.IO.MemoryMappedFiles;

namespace SeqCli.Forwarder.Filesystem.System;

sealed class SystemStoreFile : StoreFile
{
    static readonly FileStreamOptions AppendOptions = new()
    {
        Mode = FileMode.Append,
        Access = FileAccess.Write,
        Share = FileShare.ReadWrite | FileShare.Delete
    };

    readonly string _filePath;

    internal SystemStoreFile(string filePath)
    {
        _filePath = filePath;
    }

    public override bool TryGetLength([NotNullWhen(true)] out long? length)
    {
        try
        {
            length = new FileInfo(_filePath).Length;
            return true;
        }
        catch (IOException)
        {
            length = null;
            return false;
        }
    }

    public override bool TryOpenRead(long length, [NotNullWhen(true)] out StoreFileReader? reader)
    {
        MemoryMappedFile? disposeMmap = null;
        MemoryMappedViewAccessor? disposeAccessor = null;

        // If the requested length is empty then just return a dummy reader
        if (length == 0)
        {
            reader = new EmptyStoreFileReader();
            return true;
        }

        try
        {
            using var file = File.OpenHandle(_filePath, FileMode.OpenOrCreate, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, FileOptions.SequentialScan);

            disposeMmap = MemoryMappedFile.CreateFromFile(file, null, 0, MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false);
            disposeAccessor = disposeMmap.CreateViewAccessor(0, length, MemoryMappedFileAccess.Read);

            var mmap = disposeMmap;
            var accessor = disposeAccessor;

            disposeMmap = null;
            disposeAccessor = null;

            reader = new SystemStoreFileReader(mmap, accessor, length);
            return true;
        }
        // Thrown if the file length is 0
        catch (ArgumentException)
        {
            reader = null;
            return false;
        }
        // Thrown if the file is truncated while creating an accessor
        catch (UnauthorizedAccessException)
        {
            reader = null;
            return false;
        }
        // Thrown if the file is deleted
        catch (IOException)
        {
            reader = null;
            return false;
        }
        finally
        {
            disposeMmap?.Dispose();
            disposeAccessor?.Dispose();
        }
    }

    public override bool TryOpenAppend([NotNullWhen(true)] out StoreFileAppender? appender)
    {
        try
        {
            var file = File.Open(_filePath, AppendOptions);
            appender = new SystemStoreFileAppender(file);
            return true;
        }
        catch (IOException)
        {
            appender = null;
            return false;
        }
    }
}
