#nullable enable
using System.IO;
using SeqCli.Output;
using SeqCli.Tests.Support;
using Serilog.Templates.Themes;
using Xunit;

namespace SeqCli.Tests.Output;

public class OutputFormatterTests
{
    const char Escape = '\x1b';

    [Fact]
    public void ThemedJsonOutputIsColorizedRegardlessOfRedirection()
    {
        Assert.Contains(Escape, Render(TemplateTheme.Code));
    }

    [Fact]
    public void UnthemedJsonOutputIsNotColorized()
    {
        Assert.DoesNotContain(Escape, Render(theme: null));
    }

    static string Render(TemplateTheme? theme)
    {
        var evt = OutputFormat.ToSerilogEvent(Some.MakeEvent(e => e.Properties = []));

        var output = new StringWriter();
        OutputFormatter.Json(theme).Format(evt, output);
        return output.ToString();
    }
}
