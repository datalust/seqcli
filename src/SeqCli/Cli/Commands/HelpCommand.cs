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
using CommandList = System.Collections.Generic.List<Autofac.Features.Metadata.Meta<System.Lazy<SeqCli.Cli.Command>, SeqCli.Cli.CommandMetadata>>;

namespace SeqCli.Cli.Commands;

[Command("help", "Show information about available commands", Example = "seqcli help search")]
class HelpCommand : Command
{
    readonly IEnumerable<Meta<Lazy<Command>, CommandMetadata>> _availableCommands;
    bool _markdown, _pre;

    public HelpCommand(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
    {
        _availableCommands = availableCommands;
        Options.Add("pre", "Show preview commands", _ => _pre = true);
        Options.Add("m|markdown", "Generate markdown for use in documentation", _ => _markdown = true);
    }

    protected override Task<int> Run(string[] unrecognized)
    {
        var orderedCommands = _availableCommands
            .Where(c => !c.Metadata.IsPreview || _pre)
            .OrderBy(c => c.Metadata.Name)
            .ThenBy(c => c.Metadata.SubCommand)
            .ToList();
        
        var ea = Assembly.GetEntryAssembly();
        // ReSharper disable once PossibleNullReferenceException
        var name = ea!.GetName().Name!;
            
        if (_markdown)
        {
            if (unrecognized.Length != 0)
                return base.Run(unrecognized);
                
            PrintMarkdownHelp(name, orderedCommands);
            return Task.FromResult(0);
        }
            
        string? topLevelCommand = null;
        if (unrecognized.Length > 0)
        {
            topLevelCommand = unrecognized[0].ToLowerInvariant();
            var subCommand = unrecognized.Length > 1 && !unrecognized[1].Contains("-") ? unrecognized[1] : null;
            var cmds = orderedCommands.Where(c => c.Metadata.Name == topLevelCommand &&
                                                   (subCommand == null || subCommand == c.Metadata.SubCommand)).ToArray();

            if (cmds.Length == 1 && cmds[0].Metadata.SubCommand == subCommand)
            {
                var cmd = cmds.Single();
                var argHelp = cmd.Value.Value.HasArgs ? " [<args>]" : "";
                var subCommandHelp = subCommand == null ? "" : " " + subCommand;
                Console.WriteLine(name + " " + cmd.Metadata.Name + subCommandHelp + argHelp);
                Console.WriteLine();

                Console.WriteLine(cmd.Metadata.HelpText);
                Console.WriteLine();

                if (cmd.Metadata.Example != null)
                {
                    Console.WriteLine("Example:");
                    Console.WriteLine($"  {cmd.Metadata.Example}");
                    Console.WriteLine();
                }
                    
                cmd.Value.Value.PrintUsage();
                return Task.FromResult(0);
            }
        }

        if (topLevelCommand != null && orderedCommands.Any(a => a.Metadata.Name == topLevelCommand))
            PrintHelp(name, topLevelCommand, orderedCommands);
        else
            PrintHelp(name, orderedCommands);
            
        return Task.FromResult(0);
    }

    static void PrintMarkdownHelp(string executableName, CommandList orderedCommands)
    {
        Console.WriteLine("## Commands");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("```");
        Console.WriteLine($"{executableName} <command> [<args>]");
        Console.WriteLine("```");
        Console.WriteLine();

        Console.WriteLine("Available commands:");
        Console.WriteLine();

        foreach (var cmd in orderedCommands.GroupBy(cmd => cmd.Metadata.Name).OrderBy(c => c.Key))
        {
            if (cmd.Count() == 1)
            {
                var single = cmd.Single();
                if (single.Metadata.SubCommand == null)
                    Console.WriteLine($" - [`{cmd.Key}`](#{cmd.Key}) &mdash; {single.Metadata.HelpText}.");
                else
                    Console.WriteLine($" - [`{cmd.Key} {single.Metadata.SubCommand}`](#{cmd.Key}-{single.Metadata.SubCommand}) &mdash; {single.Metadata.HelpText}.");
            }
            else
            {
                Console.WriteLine($" - `{cmd.Key}`");
                foreach (var sub in cmd.OrderBy(s => s.Metadata.SubCommand))
                {
                    Console.WriteLine($"   - [`{cmd.Key} {sub.Metadata.SubCommand}`](#{cmd.Key}-{sub.Metadata.SubCommand}) &mdash; {sub.Metadata.HelpText}.");
                }
            }
        }
        Console.WriteLine();

        foreach (var cmd in orderedCommands)
        {
            if (cmd.Metadata.SubCommand != null)
                Console.WriteLine($"### `{cmd.Metadata.Name} {cmd.Metadata.SubCommand}`");
            else
                Console.WriteLine($"### `{cmd.Metadata.Name}`");
            Console.WriteLine();
            Console.WriteLine(cmd.Metadata.HelpText + ".");
            Console.WriteLine();

            if (cmd.Metadata.Example != null)
            {
                Console.WriteLine("Example:");
                Console.WriteLine();
                Console.WriteLine("```");
                Console.WriteLine(cmd.Metadata.Example);
                Console.WriteLine("```");
                Console.WriteLine();
            }

            var optionSet = cmd.Value.Value.Options;
            if (optionSet.Count == 0)
                continue;

            Console.WriteLine("| Option | Description |");
            Console.WriteLine("| ------ | ----------- |");

            foreach (var opt in optionSet)
            {
                if (opt.Hidden)
                    continue;

                Console.Write("| ");
                var zero = 0;
                optionSet.WriteOptionPrototype(Console.Out, opt, ref zero, markdown: true);
                Console.Write(" | ");
                Console.Write(opt.Description);
                Console.WriteLine(" |");
            }

            Console.WriteLine();
        }
    }

    static void PrintHelp(string executableName, CommandList orderedCommands)
    {
        Console.WriteLine($"Usage: {executableName} <command> [<args>]");
        Console.WriteLine();
        Console.WriteLine("Available commands are:");

        var printedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var avail in orderedCommands)
        {
            if (avail.Metadata.SubCommand != null)
            {
                if (!printedGroups.Contains(avail.Metadata.Name))
                {
                    Printing.Define($"  {avail.Metadata.Name}", "<sub-command>", Console.Out);
                    printedGroups.Add(avail.Metadata.Name);
                }
            }
            else
            {
                Printing.Define($"  {avail.Metadata.Name}", avail.Metadata.HelpText, Console.Out);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Type `{executableName} help <command>` for detailed help");
    }

    static void PrintHelp(string executableName, string topLevelCommand, CommandList orderedCommands)
    {
        Console.WriteLine($"Usage: {executableName} {topLevelCommand} <sub-command> [<args>]");
        Console.WriteLine();
        Console.WriteLine("Available sub-commands are:");

        foreach (var avail in orderedCommands.Where(c => c.Metadata.Name == topLevelCommand))
        {
            Printing.Define($"  {avail.Metadata.SubCommand}", avail.Metadata.HelpText, Console.Out);
        }

        Console.WriteLine();
        Console.WriteLine($"Type `{executableName} help {topLevelCommand} <sub-command>` for detailed help");
    }
}