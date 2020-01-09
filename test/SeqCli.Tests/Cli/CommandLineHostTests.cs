using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using SeqCli.Cli;
using Xunit;

namespace SeqCli.Tests.Cli
{
    public class CommandLineHostTests
    {
        [Fact]
        public async Task CheckCommandLineHostPicksCorrectCommand()
        {
            var commandsRan = new List<string>();
            var availableCommands = new List<Meta<Lazy<Command>, CommandMetadata>>
            {
                new Meta<Lazy<Command>, CommandMetadata>(
                    new Lazy<Command>(() => new ActionCommand(() => commandsRan.Add("test"))),
                    new CommandMetadata {Name = "test"}),
                new Meta<Lazy<Command>, CommandMetadata>(
                    new Lazy<Command>(() => new ActionCommand(() => commandsRan.Add("test2"))),
                    new CommandMetadata {Name = "test2"})
            };
            var commandLineHost = new CommandLineHost(availableCommands);
            await commandLineHost.Run(new []{ "test"});

            Assert.Equal("test", commandsRan.First());
        }

        [Fact]
        public async Task WhenMoreThanOneSubcommandAndTheUserRunsWithSubcommandEnsurePickedCorrect()
        {
            var commandsRan = new List<string>();
            var availableCommands =
                new List<Meta<Lazy<Command>, CommandMetadata>>
                {
                    new Meta<Lazy<Command>, CommandMetadata>(
                        new Lazy<Command>(() => new ActionCommand(() => commandsRan.Add("test-subcommand1"))),
                        new CommandMetadata {Name = "test", SubCommand = "subcommand1"}),
                    new Meta<Lazy<Command>, CommandMetadata>(
                        new Lazy<Command>(() => new ActionCommand(() => commandsRan.Add("test-subcommand2"))),
                        new CommandMetadata {Name = "test", SubCommand = "subcommand2"})
                };
            var commandLineHost = new CommandLineHost(availableCommands);
            await commandLineHost.Run(new[] { "test", "subcommand2" });

            Assert.Equal("test-subcommand2", commandsRan.First());
        }

        class ActionCommand : Command
        {
            public ActionCommand(Action action)
            {
                action.Invoke();
            }
        }
    }
}
