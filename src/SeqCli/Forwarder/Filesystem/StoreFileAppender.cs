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

namespace SeqCli.Forwarder.Filesystem;

public abstract class StoreFileAppender : IDisposable
{
    public abstract void Dispose();

    /// <summary>
    ///     Append the given data to the end of the file.
    /// </summary>
    public abstract void Append(Span<byte> data);

    /// <summary>
    ///     Commit all appended data to underlying storage.
    /// </summary>
    public abstract long Commit();

    /// <summary>
    ///     Durably sync committed data to underlying storage.
    /// </summary>
    public abstract void Sync();
}
