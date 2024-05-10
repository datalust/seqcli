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
using System.IO;

namespace SeqCli.Forwarder.Filesystem.System;

sealed class SystemStoreFileAppender : StoreFileAppender
{
    readonly FileStream _file;
    long _initialLength;
    long _written;

    internal SystemStoreFileAppender(FileStream file)
    {
        _file = file;
        _initialLength = _file.Length;
        _written = 0;
    }

    public override void Append(Span<byte> data)
    {
        _written += data.Length;
        _file.Write(data);
    }

    public override long Commit()
    {
        var writeHead = _initialLength + _written;

        _initialLength = writeHead;
        _written = 0;

        return writeHead;
    }

    public override void Sync()
    {
        _file.Flush(true);
    }

    public override void Dispose()
    {
        _file.Dispose();
    }
}
