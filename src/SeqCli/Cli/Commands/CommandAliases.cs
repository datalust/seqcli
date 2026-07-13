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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SeqCli.Cli.Commands;

static class CommandAliases
{
    public static bool RewriteArgs(
        ref string[] args, 
        [NotNullWhen(true)] out string? commandName, 
        out string? subCommandName, 
        out FeatureVisibility featureVisibility,
        out bool verbose)
    {
        if (args.Length == 0)
        {
            commandName = null;
            subCommandName = null;
            featureVisibility = FeatureVisibility.None;
            verbose = false;
            return false;
        }
        
        featureVisibility = FeatureVisibility.Visible | FeatureVisibility.Hidden;
        if (args.Any(arg => IsFlag(arg, "pre")))
        {
            featureVisibility |= FeatureVisibility.Preview;
            args = args.Where(arg => !IsFlag(arg, "pre")).ToArray();
        }

        verbose = args.Any(arg => IsFlag(arg, "verbose"));
        if (verbose)
            args = args.Where(arg => !IsFlag(arg, "verbose")).ToArray();
        
        commandName = args[0].ToLowerInvariant();
        args = args.Skip(1).ToArray();
        
        if (commandName == "--version")
        {
            commandName = "version";
        }
        else if (commandName == "--help")
        {
            commandName = "help";
        }
        
        subCommandName = commandName != "help" && args.Length != 0 && !args[0].StartsWith('-') ? args[0].ToLowerInvariant() : null;
        if (subCommandName != null)
        {
            args = args.Skip(1).ToArray();
        }
        
        if (Array.FindIndex(args, arg => IsFlag(arg, "help")) is var index and not -1)
        {
            args = args.Where((_, i) => i != index).ToArray();
            if (subCommandName != null)
            {
                args = [subCommandName, ..args];
                subCommandName = null;
            }
            args = [commandName, ..args];
            commandName = "help";
        }
        
        if (subCommandName == null && commandName == "config")
        {
            subCommandName = "legacy";
        }
        
        return true;
    }

    static bool IsFlag(string flag, string flagName)
    {
        return flag.EndsWith(flagName, StringComparison.OrdinalIgnoreCase) &&
               flag[0] == '-' &&
               (flag.Length == flagName.Length + 1 ||
                flag.Length == flagName.Length + 2 && flag[1] == '-');
    }
}
