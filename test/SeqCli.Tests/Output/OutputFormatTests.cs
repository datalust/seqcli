using System;
using SeqCli.Config;
using SeqCli.Output;
using Serilog.Sinks.SystemConsole.Themes;
using Xunit;

namespace SeqCli.Tests.Output;

public class OutputFormatTests
{
    static OutputFormat Create(
        bool? noColor = null,
        bool? forceColor = null,
        bool disableColor = false,
        bool noColorSetInEnvironment = false,
        bool outputIsRedirected = true,
        OutputSyntax syntax = OutputSyntax.Text)
        => new(
            syntax,
            noColor,
            forceColor,
            new SeqCliOutputConfig { DisableColor = disableColor },
            outputTemplate: null,
            noColorSetInEnvironment,
            outputIsRedirected);

    [Fact]
    public void RedirectedOutputIsNotThemedByDefault()
    {
        var format = Create(outputIsRedirected: true);
        Assert.Same(ConsoleTheme.None, format.Theme);
    }

    [Fact]
    public void RedirectedOutputIsThemedWhenColorIsForced()
    {
        var format = Create(forceColor: true, outputIsRedirected: true);
        Assert.Same(OutputFormat.DefaultAnsiTheme, format.Theme);
    }

    [Fact]
    public void TerminalOutputIsThemed()
    {
        var format = Create(outputIsRedirected: false);
        Assert.Same(OutputFormat.DefaultTheme, format.Theme);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoColorSuppressesTheThemeRegardlessOfRedirection(bool outputIsRedirected)
    {
        var format = Create(noColor: true, outputIsRedirected: outputIsRedirected);
        Assert.Same(ConsoleTheme.None, format.Theme);
    }

    [Fact]
    public void RedirectedJsonOutputIsNotThemedByDefault()
    {
        var format = Create(syntax: OutputSyntax.Json, outputIsRedirected: true);
        Assert.Null(format.TemplateTheme);
    }

    [Fact]
    public void RedirectedJsonOutputIsThemedWhenColorIsForced()
    {
        var format = Create(syntax: OutputSyntax.Json, forceColor: true, outputIsRedirected: true);
        Assert.NotNull(format.TemplateTheme);
    }

    // Template themes are ANSI-only, so JSON output is themed on a terminal wherever ANSI is the platform default.
    [Fact]
    public void TerminalJsonOutputIsThemedWhereverAnsiIsThePlatformDefault()
    {
        var format = Create(syntax: OutputSyntax.Json, outputIsRedirected: false);

        if (OperatingSystem.IsWindows())
            Assert.Null(format.TemplateTheme);
        else
            Assert.NotNull(format.TemplateTheme);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoColorSuppressesTheJsonThemeRegardlessOfRedirection(bool outputIsRedirected)
    {
        var format = Create(syntax: OutputSyntax.Json, noColor: true, outputIsRedirected: outputIsRedirected);
        Assert.Null(format.TemplateTheme);
    }

    [Theory]
    // noColorFlag, forceColorFlag, disableColor, noColorSetInEnvironment, expected
    [InlineData(null, null, false, false, false)] // Color is on by default.
    [InlineData(null, null, false, true, true)]   // `NO_COLOR` disables color.
    [InlineData(null, true, false, true, false)]  // `--force-color` is more specific than `NO_COLOR`.
    [InlineData(true, null, false, false, true)]  // `--no-color` disables color.
    [InlineData(true, true, false, false, true)]  // `--no-color` beats `--force-color`.
    [InlineData(null, null, true, false, true)]   // `output.disableColor` disables color.
    [InlineData(null, true, true, false, true)]   // `--force-color` doesn't override configuration.
    public void NoColorIsResolvedFromFlagsConfigurationAndEnvironment(
        bool? noColorFlag,
        bool? forceColorFlag,
        bool disableColor,
        bool noColorSetInEnvironment,
        bool expected)
    {
        var format = Create(noColorFlag, forceColorFlag, disableColor, noColorSetInEnvironment);

        Assert.Equal(expected, format.NoColor);
    }
}
