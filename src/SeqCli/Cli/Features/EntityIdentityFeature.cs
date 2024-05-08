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
using SeqCli.Util;

namespace SeqCli.Cli.Features;

class EntityIdentityFeature : CommandFeature
{
    readonly string _entityName;
    readonly string _verb;

    public EntityIdentityFeature(string entityName, string verb)
    {
        _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        _verb = verb ?? throw new ArgumentNullException(nameof(verb));
    }

    public override void Enable(OptionSet options)
    {
        options.Add(
            "t=|title=",
            $"The title of the {_entityName}(s) to {_verb}",
            t => Title = ArgumentString.Normalize(t));

        options.Add(
            "i=|id=",
            $"The id of a single {_entityName} to {_verb}",
            t => Id = ArgumentString.Normalize(t));
    }

    public override IEnumerable<string> GetUsageErrors()
    {
        if (Title != null && Id != null)
            yield return "Only one of either `title` or `id` can be specified";
    }

    public string? Title { get; private set; }

    public string? Id { get; private set; }
}