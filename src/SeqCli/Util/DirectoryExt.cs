// Copyright 2019 Datalust Pty Ltd and Contributors
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
using System.IO;

namespace SeqCli.Util
{
    static class DirectoryExt
    {
        public const char Wildcard = '*';

        public static bool IncludesWildcard(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            return filePath.Contains(Wildcard);
        }

        public static IEnumerable<string> GetFiles(string filePathWithWildcard)
        {
            if (!IncludesWildcard(filePathWithWildcard))
                throw new ArgumentException("The path does not contain a wildcard.");

            var directory = Path.GetDirectoryName(filePathWithWildcard);
            if (string.IsNullOrWhiteSpace(directory))
                directory = ".";
            else if (IncludesWildcard(directory))
                throw new ArgumentException("The wildcard may not appear in the directory path.");

            var searchPattern = Path.GetFileName(filePathWithWildcard);

            return Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
        }
    }
}
