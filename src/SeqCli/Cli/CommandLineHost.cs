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
            
            var norm = args[0].ToLowerInvariant();
            var subCommandNorm = args.Length > 1 && !args[1].Contains('-') ? args[1].ToLowerInvariant() : null;

            var pre = args.Any(a => a == prereleaseArg);
            
            var cmd = _availableCommands.SingleOrDefault(c =>
                (!c.Metadata.IsPreview || pre) &&
                c.Metadata.Name == norm &&
                (c.Metadata.SubCommand == subCommandNorm || c.Metadata.SubCommand == null));
                
            if (cmd != null)
            {
                var amountToSkip = cmd.Metadata.SubCommand == null ? 1 : 2;
                var commandSpecificArgs = args.Skip(amountToSkip).Where(arg => arg != prereleaseArg).ToArray();
                    
                var verbose = commandSpecificArgs.Any(arg => arg == verboseArg);
                if (verbose)
                {
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    commandSpecificArgs = commandSpecificArgs.Where(arg => arg != verboseArg).ToArray();
                }

                return await cmd.Value.Value.Invoke(commandSpecificArgs);
            }
        }

        Console.WriteLine($"Usage: {name} <command> [<args>]");
        Console.WriteLine($"Type `{name} help` for available commands");
        return 1;
    }
}