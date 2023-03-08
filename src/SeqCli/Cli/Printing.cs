﻿// Copyright 2018 Datalust Pty Ltd
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

using System.IO;
using System.Linq;

namespace SeqCli.Cli;

static class Printing
{
    const int ConsoleWidth = 82, TermColumnWidth = 14;

    public static void Define(string term, string definition, TextWriter output)
    {
        var header = term.PadRight(TermColumnWidth);
        var right = ConsoleWidth - header.Length;

        var rest = definition.ToCharArray();
        while (rest.Any())
        {
            var content = new string(rest.Take(right).ToArray());
            if (!string.IsNullOrWhiteSpace(content))
            {
                output.Write(header);
                header = new string(' ', header.Length);
                output.WriteLine(content);
            }
            rest = rest.Skip(right).ToArray();
        }
    }
}