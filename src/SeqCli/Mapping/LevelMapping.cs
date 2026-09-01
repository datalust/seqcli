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
using Seq.Api.Model.LogEvents;

namespace SeqCli.Mapping;

/// <summary>
/// Some Seq API
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

    // Intended only for use by ingest extraction patterns.
    public static string ToFullLevelName(string level)
    {
        return LevelsByName.TryGetValue(level, out var m) ? m : level;
    }
    
    public static LogEventLevel ToSeqApiLogEventLevel(string level)
    {
        if (string.IsNullOrEmpty(level))
            return LogEventLevel.Information;

        return ToFullLevelName(level) switch
        {
            "Trace" or "Verbose" => LogEventLevel.Verbose,
            "Debug" => LogEventLevel.Debug,
            "Warning" => LogEventLevel.Warning,
            "Error" => LogEventLevel.Error,
            "Fatal" or "Critical" or "Emergency" or "Alert" or "Panic" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
