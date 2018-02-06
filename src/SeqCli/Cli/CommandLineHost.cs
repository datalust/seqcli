using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac.Features.Metadata;

namespace SeqCli.Cli
{
    class CommandLineHost
    {
        readonly List<Meta<Lazy<Command>, CommandMetadata>> _availableCommands;

        public CommandLineHost(IEnumerable<Meta<Lazy<Command>, CommandMetadata>> availableCommands)
        {
            _availableCommands = availableCommands.ToList();
        }

        public int Run(string[] args)
        {
            var ea = Assembly.GetEntryAssembly();
            var name = ea.GetName().Name;

            if (args.Length > 0)
            {
                var norm = args[0].ToLowerInvariant();
                var cmd = _availableCommands.SingleOrDefault(c => c.Metadata.Name == norm);
                if (cmd != null)
                {
                    return cmd.Value.Value.Invoke(args.Skip(1).ToArray());
                }
            }

            Console.WriteLine($"Usage: {name} <command> [<args>]");
            Console.WriteLine($"Type `{name} help` for available commands");
            return -1;
        }
    }
}
