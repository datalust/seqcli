using System.IO;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Events;
using SeqCli.Api;
using SeqCli.Config;
using SeqCli.Output;
using SeqCli.Tests.Support;
using Xunit;

#nullable enable

namespace SeqCli.Tests.Output;

public class OutputFormatTests
{
    static OutputFormat Create(
        bool? noColor = null,
        bool? forceColor = null,
        bool disableColor = false,
        bool noColorSetInEnvironment = false,
        bool outputIsRedirected = true,
        bool supportsAnsiEscapes = true,
        OutputSyntax syntax = OutputSyntax.Text)
        => new(
            syntax,
            noColor,
            forceColor,
            new SeqCliOutputConfig { DisableColor = disableColor },
            plainTextTemplate: null,
            textEnricher: null,
            noColorSetInEnvironment,
            outputIsRedirected,
            supportsAnsiEscapes);

    [Fact]
    public void RedirectedOutputIsNotThemedByDefault()
    {
        var format = Create(outputIsRedirected: true);
        Assert.Null(format.TemplateTheme);
    }

    [Fact]
    public void RedirectedOutputIsThemedWhenColorIsForced()
    {
        var format = Create(forceColor: true, outputIsRedirected: true);
        Assert.NotNull(format.TemplateTheme);
    }

    [Fact]
    public void TerminalOutputIsThemed()
    {
        var format = Create(outputIsRedirected: false);
        Assert.NotNull(format.TemplateTheme);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoColorSuppressesTheThemeRegardlessOfRedirection(bool outputIsRedirected)
    {
        var format = Create(noColor: true, outputIsRedirected: outputIsRedirected);
        Assert.Null(format.TemplateTheme);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NoColorSuppressesTheJsonThemeRegardlessOfRedirection(bool outputIsRedirected)
    {
        var format = Create(syntax: OutputSyntax.Json, noColor: true, outputIsRedirected: outputIsRedirected);
        Assert.Null(format.TemplateTheme);
    }

    [Theory]
    // noColorFlag, forceColorFlag, disableColor, noColorSetInEnvironment, supportsAnsiEscapes, expected
    [InlineData(null, null, false, false, true, false)] // Color is on by default.
    [InlineData(null, null, false, true, true, true)]   // `NO_COLOR` disables color.
    [InlineData(null, true, false, true, true, false)]  // `--force-color` is more specific than `NO_COLOR`.
    [InlineData(true, null, false, false, true, true)]  // `--no-color` disables color.
    [InlineData(true, true, false, false, true, true)]  // `--no-color` beats `--force-color`.
    [InlineData(null, null, true, false, true, true)]   // `output.disableColor` disables color.
    [InlineData(null, true, true, false, true, true)]   // `--force-color` doesn't override configuration.
    [InlineData(null, null, false, false, false, true)] // No ANSI escape support disables color.
    [InlineData(null, true, false, false, false, true)] // ...and `--force-color` can't override it.
    public void NoColorIsResolvedFromFlagsConfigurationAndEnvironment(
        bool? noColorFlag,
        bool? forceColorFlag,
        bool disableColor,
        bool noColorSetInEnvironment,
        bool supportsAnsiEscapes,
        bool expected)
    {
        Assert.Equal(expected, OutputFormat.ResolveNoColor(noColorFlag, forceColorFlag, new SeqCliOutputConfig { DisableColor = disableColor }, noColorSetInEnvironment, supportsAnsiEscapes));
    }

    [Fact]
    public void TerminalOutputIsNotThemedWithoutAnsiEscapeSupport()
    {
        var format = Create(outputIsRedirected: false, supportsAnsiEscapes: false);
        Assert.Null(format.TemplateTheme);
    }

    static EventEntity MakeDottedHoleEvent(params (string Name, object? Value)[] properties) =>
        Some.MakeEvent(e =>
        {
            e.MessageTemplateTokens =
            [
                new MessageTemplateTokenPart { PropertyName = "user.greeting.first" },
                new MessageTemplateTokenPart { Text = " " },
                new MessageTemplateTokenPart { PropertyName = "user.name" },
                new MessageTemplateTokenPart { Text = "!" }
            ];
            e.Properties = Some.MakeProperties(properties);
        });

    static string RenderMessage(EventEntity evt)
    {
        var eventJson = EventEntityJson.ToEventJson(evt);

        var output = new StringWriter();
        TextFormatters.Plain(theme: null, "{@m}").Format(eventJson, output);
        return output.ToString();
    }

    [Fact]
    public void DottedHoleNamesResolveThroughNestedStructures()
    {
        var evt = MakeDottedHoleEvent(
            ("user", JObject.Parse("""{"greeting": {"first": "Hello"}, "name": "Barney"}""")));

        Assert.Equal("Hello Barney!", RenderMessage(evt));
    }

    [Fact]
    public void UnresolvableDottedHolesRenderAsRawText()
    {
        var evt = MakeDottedHoleEvent(("user", JObject.Parse("""{"greeting": 42}""")));

        Assert.Equal("{user.greeting.first} {user.name}!", RenderMessage(evt));
    }
}
