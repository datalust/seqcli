using System;
using System.Globalization;
using System.IO;
using Seq.Api.Model.Data;
using SeqCli.Mcp.Data;
using Serilog.Sinks.SystemConsole.Themes;

namespace SeqCli.Csv;

static class CsvWriter
{
    public static void WriteQueryResult(QueryResultPart result, ConsoleTheme theme, TextWriter output)
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
            if (first)
            {
                first = false;
                var firstCol = true;
                foreach (var heading in row)
                {
                    if (firstCol)
                    {
                        firstCol = false;
                    }
                    else
                    {
                        theme.Set(output, ConsoleThemeStyle.TertiaryText);
                        output.Write(", ");
                        theme.Reset(output);
                    }

                    WriteCell(output, theme, heading, isHeadingRow: true);
                }
            }
            else
            {
                var firstCol = true;
                foreach (var value in row)
                {
                    if (firstCol)
                    {
                        firstCol = false;
                    }
                    else
                    {
                        theme.Set(output, ConsoleThemeStyle.TertiaryText);
                        output.Write(", ");
                        theme.Reset(output);
                    }
                    
                    WriteCell(output, theme, value);
                }
            }
            output.WriteLine();
        });
    }

    static void WriteCell(TextWriter output, ConsoleTheme theme, object? value, bool isHeadingRow = false)
    {
        theme.Set(output, ConsoleThemeStyle.TertiaryText);
        output.Write('"');
        theme.Reset(output);

        var valueAsString = value switch
        {
            null => "null",
            true => "true",
            false => "false",
            decimal
                or double or float or Half
                or byte or ushort or uint or ulong or UInt128 or
                sbyte or short or int or long or Int128 => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o"),
            DateTimeOffset dto => dto.ToString("o"),
            _ => value.ToString() ?? ""
        };
        
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