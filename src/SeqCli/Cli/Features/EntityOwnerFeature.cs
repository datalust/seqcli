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

namespace SeqCli.Cli.Features;

class EntityOwnerFeature : CommandFeature
{
    readonly string _entityName;
    readonly string _verb;
    private readonly string _verbPastParticiple;
    readonly EntityIdentityFeature? _identityFeature;

    public EntityOwnerFeature(string entityName, string verb, string verbPastParticiple, EntityIdentityFeature? identityFeature = null)
    {
        _entityName = entityName ?? throw new ArgumentNullException(nameof(entityName));
        _verb = verb ?? throw new ArgumentNullException(nameof(verb));
        _verbPastParticiple = verbPastParticiple ?? throw new ArgumentNullException(nameof(verbPastParticiple));
        _identityFeature = identityFeature;
    }

    public override void Enable(OptionSet options)
    {
        options.Add(
            "o=|owner=",
            $"The id of the user to {_verb} {_entityName}s for; by default, shared {_entityName}s are {_verbPastParticiple}",
            o =>
            {
                OwnerId = StringNormalizationExtensions.Normalize(o);
                if (OwnerId != null)
                    IncludeShared = false;
            });
    }

    public override IEnumerable<string> GetUsageErrors()
    {

        if (OwnerId != null && _identityFeature?.Id != null)
        {
            yield return "Only one of either `owner` or `id` can be specified";
        }
    }

    public string? OwnerId { get; private set; }

    public bool IncludeShared { get; private set; } = true;

}