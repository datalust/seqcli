using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac.Features.Metadata;

namespace SeqCli.Cli.Commands
{
    [Command("help", "Show information about available commands")]
    class HelpCommand : Command
    {
        readonly List<Meta<Lazy<Command>, CommandMetadata>> _availableCommands;

        public HelpCommand(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
        {
            _availableCommands = availableCommands.OrderBy(c => c.Metadata.Name).ToList();
        }

        protected override int Run(string[] unrecognised)
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
                    return 0;
                }

                base.Run(unrecognised);
            }

            Console.WriteLine($"Usage: {name} <command> [<args>]");
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
            Console.WriteLine($"Type `{name} help <command>` for detailed help");

            return 0;
        }
    }
}