using System;
using System.IO;
using Seq.Api.Model.Data;
using Seq.Syntax.Templates.Themes;
using SeqCli.Mcp.Data;

namespace SeqCli.Csv;

static class CsvWriter
{
    // Delimited output is written directly rather than rendered through a template, so styled
    // runs are opened and closed here.
    static void SetStyle(TextWriter output, TemplateTheme? theme, TemplateThemeStyle style)
    {
        if (theme?.Open(style) is { } open)
            output.Write(open);
    }

    static void ResetStyle(TextWriter output, TemplateTheme? theme, TemplateThemeStyle style)
    {
        if (theme?.Close(style) is { } close)
            output.Write(close);
    }

    public static void WriteQueryResult(QueryResultPart result, Func<object?, string> stringify, TemplateTheme? theme, TextWriter output)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            SetStyle(output, theme, TemplateThemeStyle.Text);
            QueryResultHelper.WriteErrorResult(output, result);
            ResetStyle(output, theme, TemplateThemeStyle.Text);
        }
        
        var first = true;
        QueryResultHelper.Flatten(result, row =>
        {
            var firstCol = true;
            foreach (var value in row)
            {
                WriteCell(output, theme, value, stringify, ref firstCol, isHeadingRow: first);
            }
            first = false;
            output.WriteLine();
        });
    }

    static void WriteCell(TextWriter output, TemplateTheme? theme, object? value, Func<object?, string> stringify, ref bool firstCol, bool isHeadingRow = false)
    {
        if (firstCol)
        {
            firstCol = false;
        }
        else
        {
            SetStyle(output, theme, TemplateThemeStyle.TertiaryText);
            output.Write(',');
            ResetStyle(output, theme, TemplateThemeStyle.TertiaryText);
        }

        SetStyle(output, theme, TemplateThemeStyle.TertiaryText);
        output.Write('"');
        ResetStyle(output, theme, TemplateThemeStyle.TertiaryText);

        var valueAsString = stringify(value);

        var dataStyle = isHeadingRow ? TemplateThemeStyle.Name : TemplateThemeStyle.Text;
        var doubleQuote = valueAsString.IndexOf('"');
        while (doubleQuote != -1)
        {
            SetStyle(output, theme, dataStyle);
            output.Write(valueAsString[..doubleQuote]);
            ResetStyle(output, theme, dataStyle);

            SetStyle(output, theme, TemplateThemeStyle.Scalar);
            output.Write("\"\"");
            ResetStyle(output, theme, TemplateThemeStyle.Scalar);

            valueAsString = valueAsString[(doubleQuote + 1)..];
            doubleQuote = valueAsString.IndexOf('"');
        }

        SetStyle(output, theme, dataStyle);
        output.Write(valueAsString);
        ResetStyle(output, theme, dataStyle);

        SetStyle(output, theme, TemplateThemeStyle.TertiaryText);
        output.Write('"');
        ResetStyle(output, theme, TemplateThemeStyle.TertiaryText);
    }
}