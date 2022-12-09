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

namespace SeqCli.Cli.Features
{
    class UserIdentityFeature : CommandFeature
    {
        readonly string _verb;

        string? _name;
        string? _id;

        public UserIdentityFeature(string verb)
        {
            _verb = verb ?? throw new ArgumentNullException(nameof(verb));
        }

        public override void Enable(OptionSet options)
        {
            options.Add(
                "n=|name=",
                $"The username of the user(s) to {_verb}",
                t => _name = t);

            options.Add(
                "i=|id=",
                $"The id of a single user to {_verb}",
                t => _id = t);
        }

        public override IEnumerable<string> GetUsageErrors()
        {
            if (Name != null && Id != null)
                yield return "Only one of either `name` or `id` can be specified";
        }

        public string? Name => string.IsNullOrWhiteSpace(_name) ? null : _name.Trim();

        public string? Id => string.IsNullOrWhiteSpace(_id) ? null : _id.Trim();
    }
}
