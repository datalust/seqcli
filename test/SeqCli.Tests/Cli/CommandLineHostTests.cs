using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using SeqCli.Cli;
using SeqCli.Cli.Features;
using SeqCli.Tests.Support;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace SeqCli.Tests.Cli;

public class CommandLineHostTests
{
    [Fact]
    public async Task CommandLineHostPicksCorrectCommand()
    {
        var executed = new List<string>();
        var availableCommands = new List<Meta<Lazy<Command>, CommandMetadata>>
        {
            new(
                new Lazy<Command>(() => new ActionCommand(() => executed.Add("test"))),
                new CommandMetadata {Name = "test", HelpText = "help"}),
            new(
                new Lazy<Command>(() => new ActionCommand(() => executed.Add("test2"))),
                new CommandMetadata {Name = "test2", HelpText = "help"})
        };
        var commandLineHost = new CommandLineHost(availableCommands);
        await commandLineHost.Run(["test"],new LoggingLevelSwitch());

        Assert.Equal("test", executed.Single());
    } 
    
    [Fact]
    public async Task PrereleaseCommandsAreIgnoredWithoutFlag()
    {
        var executed = new List<string>();
        var availableCommands = new List<Meta<Lazy<Command>, CommandMetadata>>
        {
            new(
                new Lazy<Command>(() => new ActionCommand(() => executed.Add("test"))),
                new CommandMetadata {Name = "test", HelpText = "help", IsPreview = true}),
        };
        var commandLineHost = new CommandLineHost(availableCommands);
        var exit = await commandLineHost.Run(["test"],new LoggingLevelSwitch());
        Assert.Equal(1, exit);
        Assert.Empty(executed);

        exit = await commandLineHost.Run(["test", "--pre"],new LoggingLevelSwitch());
        Assert.Equal(0, exit);
        Assert.Equal("test", executed.Single());
    }

    [Fact]
    public async Task WhenMoreThanOneSubcommandAndTheUserRunsWithSubcommandEnsurePickedCorrect()
    {
        var executed = new List<string>();
        var availableCommands =
            new List<Meta<Lazy<Command>, CommandMetadata>>
            {
                new(
                    new Lazy<Command>(() => new ActionCommand(() => executed.Add("test-subcommand1"))),
                    new CommandMetadata {Name = "test", SubCommand = "subcommand1", HelpText = "help"}),
                new(
                    new Lazy<Command>(() => new ActionCommand(() => executed.Add("test-subcommand2"))),
                    new CommandMetadata {Name = "test", SubCommand = "subcommand2", HelpText = "help"})
            };
        var commandLineHost = new CommandLineHost(availableCommands);
        await commandLineHost.Run(["test", "subcommand2"], new LoggingLevelSwitch());

        Assert.Equal("test-subcommand2", executed.First());
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
                    new CommandMetadata {Name = "test", HelpText = "help"})
            };
            
        var commandLineHost = new CommandLineHost(availableCommands);
            
        await commandLineHost.Run(["test", "--verbose"], levelSwitch);
            
        Assert.Equal(LogEventLevel.Information, levelSwitch.MinimumLevel);
    }
}