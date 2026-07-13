using System;
using System.IO;
using Seq.Api.Model.Data;
using SeqCli.Mcp.Data;
using Serilog.Sinks.SystemConsole.Themes;

namespace SeqCli.Csv;

static class CsvWriter
{
    public static void WriteQueryResult(QueryResultPart result, Func<object?, string> stringify, ConsoleTheme theme, TextWriter output)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            theme.Set(output, ConsoleThemeStyle.Text);
            QueryResultHelper.WriteErrorResult(output, result);
            theme.Reset(output);
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

    static void WriteCell(TextWriter output, ConsoleTheme theme, object? value, Func<object?, string> stringify, ref bool firstCol, bool isHeadingRow = false)
    {
        if (firstCol)
        {
            firstCol = false;
        }
        else
        {
            theme.Set(output, ConsoleThemeStyle.TertiaryText);
            output.Write(',');
            theme.Reset(output);
        }
        
        theme.Set(output, ConsoleThemeStyle.TertiaryText);
        output.Write('"');
        theme.Reset(output);

        var valueAsString = stringify(value);
        
        var dataStyle = isHeadingRow ? ConsoleThemeStyle.Name : ConsoleThemeStyle.Text;
        var doubleQuote = valueAsString.IndexOf('"');
        while (doubleQuote != -1)
        {
            theme.Set(output, dataStyle);
            output.Write(valueAsString[..doubleQuote]);
            theme.Reset(output);
            
            theme.Set(output, ConsoleThemeStyle.Scalar);
            output.Write("\"\"");
            theme.Reset(output);

            valueAsString = valueAsString[(doubleQuote + 1)..];
            doubleQuote = valueAsString.IndexOf('"');
        }
        
        theme.Set(output, dataStyle);
        output.Write(valueAsString);
        theme.Reset(output);
        
        theme.Set(output, ConsoleThemeStyle.TertiaryText);
        output.Write('"');
        theme.Reset(output);
    }
}