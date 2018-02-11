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

namespace SeqCli.Cli
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute, ICommandMetadata
    {
        public string Name { get; }
        public string HelpText { get; }

        public string Example { get; set; }

        public CommandAttribute(string name, string helpText)
        {
            Name = name;
            HelpText = helpText;
        }
    }
}
