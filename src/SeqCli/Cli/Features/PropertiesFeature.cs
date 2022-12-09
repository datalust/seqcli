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

using System.Collections.Generic;

#nullable enable

namespace SeqCli.Cli.Features;

class PropertiesFeature : CommandFeature
{
    readonly string _shortOptionName;
    readonly string _longOptionName;
    readonly string _description;
    readonly Dictionary<string, object?> _properties = new();

    public IReadOnlyDictionary<string, object?> Properties => _properties;

    public PropertiesFeature()
        : this("p", "property", "Specify name/value properties, e.g. `-p Customer=C123 -p Environment=Production`")
    {
    }

    public PropertiesFeature(string shortOptionName, string longOptionName, string description)
    {
        _shortOptionName = shortOptionName;
        _longOptionName = longOptionName;
        _description = description;
    }

    public override void Enable(OptionSet options)
    {
        options.Add(
            _shortOptionName + "={=}|" + _longOptionName + "={=}",
            _description,
            (n, v) =>
            {
                var name = n.Trim();
                var valueText = v?.Trim();
                if (string.IsNullOrEmpty(valueText))
                    _properties.Add(name, null);
                else if (valueText == "true")
                    _properties.Add(name, true);
                else if (valueText == "false")
                    _properties.Add(name, false);
                else if (decimal.TryParse(valueText, out var numeric))
                    _properties.Add(name, numeric);
                else
                    _properties.Add(name, valueText);
            });
    }
}