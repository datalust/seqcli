#nullable enable
using System;
using System.IO;
using Seq.Api.Model.Data;
using SeqCli.Csv;
using Seq.Syntax.Templates.Themes;
using Xunit;

namespace SeqCli.Tests.Csv;

public class CsvWriterTests
{
    const char Escape = '\x1b';

    // `CsvWriter` writes delimited output directly rather than rendering a template, so unlike the
    // other output paths it applies (or omits) the theme itself.
    [Fact]
    public void QueryResultsAreNotColorizedWhenOutputIsRedirected()
    {
        var rendered = Render(RedirectedTheme(forceColor: false));

        Assert.DoesNotContain(Escape, rendered);
        Assert.Equal($"\"Events\",\"Application\"{Environment.NewLine}\"852\",\"Roastery \"\"Web\"\" Frontend\"{Environment.NewLine}", rendered);
    }

    [Fact]
    public void QueryResultsAreColorizedWhenColorIsForcedForRedirectedOutput()
    {
        Assert.Contains(Escape, Render(RedirectedTheme(forceColor: true)));
    }

    [Fact]
    public void QueryErrorsAreNotColorizedWhenOutputIsRedirected()
    {
        var rendered = Render(RedirectedTheme(forceColor: false), new QueryResultPart
        {
            Error = "The query could not be executed.",
            Reasons = []
        });

        Assert.DoesNotContain(Escape, rendered);
        Assert.Contains("The query could not be executed.", rendered);
    }

    static TemplateTheme? RedirectedTheme(bool forceColor) =>
        forceColor ? TemplateTheme.Code : null;

    static string Render(TemplateTheme? theme, QueryResultPart? result = null)
    {
        result ??= new QueryResultPart
        {
            Columns = ["Events", "Application"],
            Rows = [[852, "Roastery \"Web\" Frontend"]]
        };

        var output = new StringWriter();
        CsvWriter.WriteQueryResult(result, v => v?.ToString() ?? "null", theme, output);
        return output.ToString();
    }
}
