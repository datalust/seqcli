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
            var name = ea.GetName().Name;

            if (unrecognised.Length > 0)
            {
                var target = unrecognised[0].ToLowerInvariant();
                var cmd = _availableCommands.SingleOrDefault(c => c.Metadata.Name == target);
                if (cmd != null)
                {
                    var argHelp = cmd.Value.Value.HasArgs ? " [<args>]" : "";
                    Console.WriteLine(name + " " + cmd.Metadata.Name + argHelp);
                    Console.WriteLine();
                    Console.WriteLine(cmd.Metadata.HelpText);
                    Console.WriteLine();
                    cmd.Value.Value.PrintUsage();
                    return Task.FromResult(0);
                }

                return base.Run(unrecognised);
            }

            if (_markdown)
            {
                PrintMarkdownHelp(name);
            }
            else
            {
                PrintHelp(name);
            }

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

            foreach (var cmd in _availableCommands)
            {
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

            foreach (var availableCommand in _availableCommands)
            {
                Printing.Define(
                    "  " + availableCommand.Metadata.Name,
                    availableCommand.Metadata.HelpText,
                    13,
                    Console.Out);
            }

            Console.WriteLine();
            Console.WriteLine($"Type `{executableName} help <command>` for detailed help");
        }
    }
}