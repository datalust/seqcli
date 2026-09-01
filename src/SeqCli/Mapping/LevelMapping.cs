// Copyright © Datalust and contributors.
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

namespace SeqCli.Mapping;

/// <summary>
/// Recognizes the level spellings found in event data from various sources (<c>info</c>,
/// <c>WARN</c>, <c>trce</c>, …) and maps them to canonical Seq level names. Level values
/// themselves are preserved verbatim throughout the pipeline; the canonical name is used
/// where a normalized form is needed.
/// </summary>
public static class LevelMapping
{
    static readonly Dictionary<string, string> LevelsByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["t"] = "Trace",
            ["tr"] = "Trace",
            ["trc"] = "Trace",
            ["trce"] = "Trace",
            ["trace"] = "Trace",
            ["v"] = "Verbose",
            ["ver"] = "Verbose",
            ["vrb"] = "Verbose",
            ["verb"] = "Verbose",
            ["verbose"] = "Verbose",
            ["d"] = "Debug",
            ["de"] = "Debug",
            ["dbg"] = "Debug",
            ["deb"] = "Debug",
            ["dbug"] = "Debug",
            ["debu"] = "Debug",
            ["debug"] = "Debug",
            ["i"] = "Information",
            ["in"] = "Information",
            ["inf"] = "Information",
            ["info"] = "Information",
            ["information"] = "Information",
            ["notice"] = "Notice",
            ["w"] = "Warning",
            ["wa"] = "Warning",
            ["war"] = "Warning",
            ["wrn"] = "Warning",
            ["warn"] = "Warning",
            ["warning"] = "Warning",
            ["e"] = "Error",
            ["er"] = "Error",
            ["err"] = "Error",
            ["erro"] = "Error",
            ["eror"] = "Error",
            ["error"] = "Error",
            ["f"] = "Fatal",
            ["fa"] = "Fatal",
            ["ftl"] = "Fatal",
            ["fat"] = "Fatal",
            ["fatl"] = "Fatal",
            ["fatal"] = "Fatal",
            ["c"] = "Critical",
            ["cr"] = "Critical",
            ["crt"] = "Critical",
            ["cri"] = "Critical",
            ["crit"] = "Critical",
            ["critical"] = "Critical",
            ["emerg"] = "Emergency",
            ["alert"] = "Alert",
            ["panic"] = "Panic"
        };

    public static string ToFullLevelName(string level)
    {
        return LevelsByName.TryGetValue(level, out var m) ? m : level;
    }
}
