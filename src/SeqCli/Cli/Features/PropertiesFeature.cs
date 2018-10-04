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

namespace SeqCli.Cli.Features
{
    class PropertiesFeature : CommandFeature
    {
        readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

        public IReadOnlyDictionary<string, object> Properties => _properties;

        public override void Enable(OptionSet options)
        {
            options.Add(
                "p={=}|property={=}",
                "Specify name/value properties, e.g. `-p Customer=C123 -p Environment=Production`",
                (n, v) =>
                {
                    var name = n.Trim();
                    var valueText = v?.Trim();
                    if (string.IsNullOrEmpty(valueText))
                        _properties.Add(name, null);
                    else if (decimal.TryParse(valueText, out var numeric))
                        _properties.Add(name, numeric);
                    else
                        _properties.Add(name, valueText);
                });
        }
    }
}
