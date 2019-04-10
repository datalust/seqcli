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

using System;
using System.Collections.Generic;
using System.IO;
using SeqCli.Util;

namespace SeqCli.Cli.Features
{
    class FileInputFeature : CommandFeature
    {
        readonly string _description;
        readonly bool _supportsWildcard;

        public FileInputFeature(string description, bool supportsWildcard = false)
        {
            _description = description;
            _supportsWildcard = supportsWildcard;
        }

        public string InputFilename { get; private set; }

        public override void Enable(OptionSet options)
        {
            var wildcardHelp = _supportsWildcard ? $", including the `{DirectoryExt.Wildcard}` wildcard" : "";
            options.Add("i=|input=",
                $"{_description}{wildcardHelp}; if not specified, `STDIN` will be used",
                v => InputFilename = string.IsNullOrWhiteSpace(v) ? null : v.Trim());
        }

        static TextReader OpenText(string filename)
        {
            return new StreamReader(
                File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public TextReader OpenInput()
        {
            return InputFilename != null ? OpenText(InputFilename) : Console.In;
        }

        public IEnumerable<TextReader> OpenInputs()
        {
            if (InputFilename == null || !DirectoryExt.IncludesWildcard(InputFilename))
            {
                yield return OpenInput();
            }
            else
            {
                foreach (var path in DirectoryExt.GetFiles(InputFilename))
                    yield return OpenText(path);
            }
        }
    }
}
