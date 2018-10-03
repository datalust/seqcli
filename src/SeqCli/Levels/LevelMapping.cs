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
using Serilog.Events;

namespace SeqCli.Levels
{
    public static class LevelMapping
    {
        static readonly Dictionary<string, (string, LogEventLevel)> LevelsByName =
            new Dictionary<string, (string, LogEventLevel)>(StringComparer.OrdinalIgnoreCase)
        {
            ["t"] = ("Trace", LogEventLevel.Verbose),
            ["tr"] = ("Trace", LogEventLevel.Verbose),
            ["trc"] = ("Trace", LogEventLevel.Verbose),
            ["trce"] = ("Trace", LogEventLevel.Verbose),
            ["trace"] = ("Trace", LogEventLevel.Verbose),
            ["v"] = ("Verbose", LogEventLevel.Verbose),
            ["ver"] = ("Verbose", LogEventLevel.Verbose),
            ["vrb"] = ("Verbose", LogEventLevel.Verbose),
            ["verb"] = ("Verbose", LogEventLevel.Verbose),
            ["verbose"] = ("Verbose", LogEventLevel.Verbose),
            ["d"] = ("Debug", LogEventLevel.Debug),
            ["de"] = ("Debug", LogEventLevel.Debug),
            ["dbg"] = ("Debug", LogEventLevel.Debug),
            ["deb"] = ("Debug", LogEventLevel.Debug),
            ["dbug"] = ("Debug", LogEventLevel.Debug),
            ["debu"] = ("Debug", LogEventLevel.Debug),
            ["debug"] = ("Debug", LogEventLevel.Debug),
            ["i"] = ("Information", LogEventLevel.Information),
            ["in"] = ("Information", LogEventLevel.Information),
            ["inf"] = ("Information", LogEventLevel.Information),
            ["info"] = ("Information", LogEventLevel.Information),
            ["information"] = ("Information", LogEventLevel.Information),
            ["w"] = ("Warning", LogEventLevel.Warning),
            ["wa"] = ("Warning", LogEventLevel.Warning),
            ["war"] = ("Warning", LogEventLevel.Warning),
            ["wrn"] = ("Warning", LogEventLevel.Warning),
            ["warn"] = ("Warning", LogEventLevel.Warning),
            ["warning"] = ("Warning", LogEventLevel.Warning),
            ["e"] = ("Error", LogEventLevel.Error),
            ["er"] = ("Error", LogEventLevel.Error),
            ["err"] = ("Error", LogEventLevel.Error),
            ["erro"] = ("Error", LogEventLevel.Error),
            ["eror"] = ("Error", LogEventLevel.Error),
            ["error"] = ("Error", LogEventLevel.Error),
            ["f"] = ("Fatal", LogEventLevel.Fatal),
            ["fa"] = ("Fatal", LogEventLevel.Fatal),
            ["ftl"] = ("Fatal", LogEventLevel.Fatal),
            ["fat"] = ("Fatal", LogEventLevel.Fatal),
            ["fatl"] = ("Fatal", LogEventLevel.Fatal),
            ["fatal"] = ("Fatal", LogEventLevel.Fatal),
            ["c"] = ("Critical", LogEventLevel.Fatal),
            ["cr"] = ("Critical", LogEventLevel.Fatal),
            ["crt"] = ("Critical", LogEventLevel.Fatal),
            ["cri"] = ("Critical", LogEventLevel.Fatal),
            ["crit"] = ("Critical", LogEventLevel.Fatal),
            ["critical"] = ("Critical", LogEventLevel.Fatal)
        };

        public static LogEventLevel ToSerilogLevel(string level)
        {
            return LevelsByName.TryGetValue(level, out var m) ? m.Item2 : LogEventLevel.Information;
        }

        public static string ToFullLevelName(string level)
        {
            return LevelsByName.TryGetValue(level, out var m) ? m.Item1 : level;
        }
    }
}