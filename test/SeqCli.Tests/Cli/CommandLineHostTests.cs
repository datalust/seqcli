using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using SeqCli.Cli;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace SeqCli.Tests.Cli;

public class CommandLineHostTests
{
    [Fact]
    public async Task CheckCommandLineHostPicksCorrectCommand()
    {
        var executed = new List<string>();
        var availableCommands = new List<Meta<Lazy<Command>, CommandMetadata>>
        {
            new(
                new Lazy<Command>(() => new ActionCommand(() => executed.Add("test"))),
                new CommandMetadata {Name = "test"}),
            new(
                new Lazy<Command>(() => new ActionCommand(() => executed.Add("test2"))),
                new CommandMetadata {Name = "test2"})
        };
        var commandLineHost = new CommandLineHost(availableCommands);
        await commandLineHost.Run(new []{ "test"},new LoggingLevelSwitch());

        Assert.Equal("test", executed.First());
    }

    [Fact]
    public async Task WhenMoreThanOneSubcommandAndTheUserRunsWithSubcommandEnsurePickedCorrect()
    {
        var commandsRan = new List<string>();
        var availableCommands =
            new List<Meta<Lazy<Command>, CommandMetadata>>
            {
                new(
                    new Lazy<Command>(() => new ActionCommand(() => commandsRan.Add("test-subcommand1"))),
                    new CommandMetadata {Name = "test", SubCommand = "subcommand1"}),
                new(
                    new Lazy<Command>(() => new ActionCommand(() => commandsRan.Add("test-subcommand2"))),
                    new CommandMetadata {Name = "test", SubCommand = "subcommand2"})
            };
        var commandLineHost = new CommandLineHost(availableCommands);
        await commandLineHost.Run(new[] { "test", "subcommand2" }, new LoggingLevelSwitch());

        Assert.Equal("test-subcommand2", commandsRan.First());
    }

    [Fact]
    public async Task VerboseOptionSetsLoggingLevelToInformation()
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
            
        var availableCommands =
            new List<Meta<Lazy<Command>, CommandMetadata>>
            {
                new(
                    new Lazy<Command>(() => new ActionCommand(() => { })),
                    new CommandMetadata {Name = "test"})
            };
            
        var commandLineHost = new CommandLineHost(availableCommands);
            
        await commandLineHost.Run(new[] { "test", "--verbose" }, levelSwitch);
            
        Assert.Equal(LogEventLevel.Information, levelSwitch.MinimumLevel);
    }

    class ActionCommand : Command
    {
        public ActionCommand(Action action)
        {
            action.Invoke();
        }
    }
}