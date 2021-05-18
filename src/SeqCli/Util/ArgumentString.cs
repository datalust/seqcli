// Copyright 2018 Datalust Pty Ltd
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

using System.Linq;

#nullable enable

namespace SeqCli.Util
{
    static class ArgumentString
    {
        public static string? Normalize(string? argument)
        {
            return string.IsNullOrWhiteSpace(argument) ? null : argument.Trim();
        }

        public static string[] NormalizeList(string? argument)
        {
            return (argument ?? "")
                .Split(',')
                .Select(Normalize)
                .Where(s => s != null)
                .ToArray()!;
        }
    }
}
