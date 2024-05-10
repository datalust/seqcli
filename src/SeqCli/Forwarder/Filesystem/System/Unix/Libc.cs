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

#if UNIX
using System.Runtime.InteropServices;

namespace SeqCli.Forwarder.Filesystem.System.Unix;

static class Libc
{
    [DllImport("libc")]
    public static extern int open(string path, int flags);

    [DllImport("libc")]
    public static extern int close(int fd);

    [DllImport("libc")]
    public static extern int fsync(int fd);
}
#endif
