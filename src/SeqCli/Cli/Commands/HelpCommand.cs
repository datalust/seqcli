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

namespace SeqCli.Cli.Commands
{
    [Command("help", "Show information about available commands", Example = "seqcli help search")]
    class HelpCommand : Command
    {
        readonly List<Meta<Lazy<Command>, CommandMetadata>> _availableCommands;
        bool _markdown;

        public HelpCommand(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
        {
            Options.Add("m|markdown", "Generate markdown for use in documentation", _ => _markdown = true);
            _availableCommands = availableCommands.OrderBy(c => c.Metadata.Name).ToList();
        }

        protected override Task<int> Run(string[] unrecognised)
        {
            var ea = Assembly.GetEntryAssembly();
            // ReSharper disable once PossibleNullReferenceException
            var name = ea.GetName().Name;
            
            if (_markdown)
            {
                if (unrecognised.Length != 0)
                    return base.Run(unrecognised);
                
                PrintMarkdownHelp(name);
                return Task.FromResult(0);
            }
            
            string topLevelCommand = null;
            if (unrecognised.Length > 0)
            {
                topLevelCommand = unrecognised[0].ToLowerInvariant();
                var subCommand = unrecognised.Length > 1 && !unrecognised[1].Contains("-") ? unrecognised[1] : null;
                var cmds = _availableCommands.Where(c => c.Metadata.Name == topLevelCommand &&
                                                         (subCommand == null || subCommand == c.Metadata.SubCommand)).ToArray();
                if (cmds.Length == 0)
                    return base.Run(unrecognised);

                if (cmds.Length == 1)
                {
                    var cmd = cmds.Single();
                    var argHelp = cmd.Value.Value.HasArgs ? " [<args>]" : "";
                    var subCommandHelp = subCommand == null ? "" : " " + subCommand;
                    Console.WriteLine(name + " " + cmd.Metadata.Name + subCommandHelp + argHelp);
                    Console.WriteLine();
                    Console.WriteLine(cmd.Metadata.HelpText);
                    Console.WriteLine();
                    cmd.Value.Value.PrintUsage();
                    return Task.FromResult(0);
                }
            }

            if (topLevelCommand != null)
                PrintHelp(name, topLevelCommand);
            else
                PrintHelp(name);
            
            return Task.FromResult(0);
        }

        void PrintMarkdownHelp(string executableName)
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

            foreach (var cmd in _availableCommands.GroupBy(cmd => cmd.Metadata.Name).OrderBy(c => c.Key))
            {
                if (cmd.Count() == 1)
                {
                    Console.WriteLine($" - [`{cmd.Key}`](#{cmd.Key}) &mdash; {cmd.Single().Metadata.HelpText}.");
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

            foreach (var cmd in _availableCommands)
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

        void PrintHelp(string executableName)
        {
            Console.WriteLine($"Usage: {executableName} <command> [<args>]");
            Console.WriteLine();
            Console.WriteLine("Available commands are:");

            var printedGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var avail in _availableCommands.OrderBy(c => c.Metadata.Name))
            {
                if (avail.Metadata.SubCommand != null)
                {
                    if (!printedGroups.Contains(avail.Metadata.Name))
                    {
                        Printing.Define($"  {avail.Metadata.Name}", "<sub-command>", 13, Console.Out);
                        printedGroups.Add(avail.Metadata.Name);
                    }
                }
                else
                {
                    Printing.Define($"  {avail.Metadata.Name}", avail.Metadata.HelpText, 13, Console.Out);
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Type `{executableName} help <command>` for detailed help");
        }

        void PrintHelp(string executableName, string topLevelCommand)
        {
            Console.WriteLine($"Usage: {executableName} {topLevelCommand} <sub-command> [<args>]");
            Console.WriteLine();
            Console.WriteLine("Available sub-commands are:");

            foreach (var avail in _availableCommands.Where(c => c.Metadata.Name == topLevelCommand).OrderBy(c => c.Metadata.SubCommand))
            {
                Printing.Define($"  {avail.Metadata.SubCommand}", avail.Metadata.HelpText, 13, Console.Out);
            }

            Console.WriteLine();
            Console.WriteLine($"Type `{executableName} help {topLevelCommand} <sub-command>` for detailed help");
        }
    }
}