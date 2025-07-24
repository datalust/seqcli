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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Cli;

class CommandLineHost
{
    readonly List<Meta<Lazy<Command>, CommandMetadata>> _availableCommands;

    public CommandLineHost(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
    {
        _availableCommands = availableCommands.ToList();
    }

    public async Task<int> Run(string[] args, LoggingLevelSwitch levelSwitch)
    {
        var ea = Assembly.GetEntryAssembly();
        var name = ea!.GetName().Name;

        if (args.Length > 0)
        {
            const string prereleaseArg = "--pre", verboseArg = "--verbose";
            
            var commandName = args[0].ToLowerInvariant();
            var subCommandName = args.Length > 1 && !args[1].Contains('-') ? args[1].ToLowerInvariant() : null;

            var hiddenLegacyCommand = false;
            if (subCommandName == null && commandName == "config")
            {
                hiddenLegacyCommand = true;
                subCommandName = "legacy";
            }
            
            var featureVisibility = FeatureVisibility.Visible | FeatureVisibility.Hidden;
            if (args.Any(a => a.Trim() is prereleaseArg))
                featureVisibility |= FeatureVisibility.Preview;
            
            var currentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? SupportedPlatforms.Windows
                : SupportedPlatforms.Unix;
            
            var cmd = _availableCommands.SingleOrDefault(c =>
                c.Metadata.Platforms.HasFlag(currentPlatform) && featureVisibility.HasFlag(c.Metadata.Visibility) &&
                c.Metadata.Name == commandName &&
                (c.Metadata.SubCommand == subCommandName || c.Metadata.SubCommand == null));
                
            if (cmd != null)
            {
                var amountToSkip = cmd.Metadata.SubCommand == null || hiddenLegacyCommand ? 1 : 2;
                var commandSpecificArgs = args.Skip(amountToSkip).Where(arg => cmd.Metadata.Name == "help" || arg is not prereleaseArg).ToArray();
                
                var verbose = commandSpecificArgs.Any(arg => arg == verboseArg);
                if (verbose)
                {
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    commandSpecificArgs = commandSpecificArgs.Where(arg => arg != verboseArg).ToArray();
                }

                var impl = cmd.Value.Value;
                return await impl.Invoke(commandSpecificArgs);
            }
        }

        Console.WriteLine($"Usage: {name} <command> [<args>]");
        Console.WriteLine($"Type `{name} help` for available commands");
        return 1;
    }
}