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
using System.Text;

namespace SeqCli.Output;

static class PrintTemplate
{
    /// <summary>
    /// Windows disallows passing literal newlines in process arguments (and it's inconvenient to figure out
    /// how to do it with varied shells, anyway). This function provides JSON-like (C#-like) backslash-escaped
    /// substitutes for newlines and a few other nonprintable characters.
    /// </summary>
    public static string InterpretEscapeChars(string template)
    {
        var result = new StringBuilder();
        for (var i = 0; i < template.Length; ++i)
        {
            var ch = template[i];
            if (ch != '\\')
            {
                result.Append(ch);
                continue;
            }

            if (i == template.Length - 1)
                throw new ArgumentException(@"Trailing literal `\` is invalid in templates and must be escaped as `\\`.");

            i += 1;
            var next = template[i];
            result.Append(next switch
            {
                '\\' => '\\',
                't' => '\t',
                'r' => '\r',
                'n' => '\n',
                'e' => '\e',
                'b' => '\b',
                'f' => '\f',
                _ => throw new ArgumentException($@"Sequence `\{next}` is invalid in templates; escape literal `\` as `\\`.")
            });
        }

        return result.ToString();
    }
}