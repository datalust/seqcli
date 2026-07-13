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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using SeqCli.Cli.Commands;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Cli;

class CommandLineHost(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
{
    readonly List<Meta<Lazy<Command>, CommandMetadata>> _availableCommands = availableCommands.ToList();

    public async Task<int> Run(string[] args, LoggingLevelSwitch levelSwitch)
    {
        var ea = Assembly.GetEntryAssembly();
        var name = ea!.GetName().Name;

        if (CommandAliases.RewriteArgs(
                ref args,
                out var commandName,
                out var subCommandName,
                out var featureVisibility,
                out var verbose))
        {
            var currentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? SupportedPlatforms.Windows
                : SupportedPlatforms.Posix;
            
            var cmd = _availableCommands.SingleOrDefault(c =>
                c.Metadata.Platforms.HasFlag(currentPlatform) && featureVisibility.HasFlag(c.Metadata.Visibility) &&
                c.Metadata.Name == commandName &&
                (c.Metadata.SubCommand == subCommandName || c.Metadata.SubCommand == null));
                
            if (cmd != null)
            {
                if (verbose)
                    levelSwitch.MinimumLevel = LogEventLevel.Information;

                var impl = cmd.Value.Value;
                return await impl.Invoke(args);
            }
        }
        
        Console.WriteLine($"Usage: {name} <command> [<args>]");
        Console.WriteLine($"Type `{name} help` for available commands");
        return 1;
    }
}
