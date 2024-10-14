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
using System.Collections.Generic;
using System.Linq;

namespace SeqCli.Config;

static class EnvironmentOverrides
{
    public static void Apply(string prefix, SeqCliConfig config)
    {
        var environment = Environment.GetEnvironmentVariables();
        Apply(prefix, config, environment.Keys.Cast<string>().ToDictionary(k => k, k => (string?)environment[k]));
    }

    internal static void Apply(string prefix, SeqCliConfig config, Dictionary<string, string?> environment)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (environment == null) throw new ArgumentNullException(nameof(environment));

        config.DisallowExport();

        foreach (var (key, _, _) in KeyValueSettings.Inspect(config))
        {
            var envVar = ToEnvironmentVariableName(prefix, key);
            if (environment.TryGetValue(envVar, out var value))
            {
                KeyValueSettings.Set(config, key, value ?? "");
            }
        }
    }

    static string ToEnvironmentVariableName(string prefix, string key)
    {
        return prefix + key.Replace(".", "_").ToUpperInvariant();
    }
}
