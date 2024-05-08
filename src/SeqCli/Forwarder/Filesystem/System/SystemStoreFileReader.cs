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
using System.IO.MemoryMappedFiles;

namespace SeqCli.Forwarder.Filesystem.System;

public sealed class SystemStoreFileReader : StoreFileReader
{
    readonly MemoryMappedViewAccessor _accessor;
    readonly MemoryMappedFile _file;
    readonly long _length;

    internal SystemStoreFileReader(MemoryMappedFile file, MemoryMappedViewAccessor accessor, long length)
    {
        _file = file;

        _accessor = accessor;
        _length = length;
    }

    public override long CopyTo(Span<byte> buffer, long from = 0, long? length = null)
    {
        unsafe
        {
            var ptr = (byte*)_accessor.SafeMemoryMappedViewHandle.DangerousGetHandle();
            var memmap = new Span<byte>(ptr + from, (int)(length ?? _length));

            memmap.CopyTo(buffer);

            return memmap.Length;
        }
    }

    public override void Dispose()
    {
        _accessor.Dispose();
        _file.Dispose();
    }
}
