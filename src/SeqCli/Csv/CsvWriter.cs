using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Sinks.SystemConsole.Themes;
using Superpower.Model;

namespace SeqCli.Csv;

static class CsvWriter
{
    public static void WriteCsv(IEnumerable<Token<CsvToken>> csv, ConsoleTheme theme, TextWriter output, bool hasHeaderRow)
    {
        var isHeaderRow = hasHeaderRow;
            
        foreach (var token in csv)
        {
            switch (token.Kind)
            {
                case CsvToken.Newline:
                    output.WriteLine();
                    isHeaderRow = false;
                    break;
                case CsvToken.Comma:
                    theme.Set(output, ConsoleThemeStyle.TertiaryText);
                    output.Write(',');
                    theme.Reset(output);
                    break;
                case CsvToken.DoubleQuote:
                    theme.Set(output, ConsoleThemeStyle.TertiaryText);
                    output.Write('"');
                    theme.Reset(output);
                    break;
                case CsvToken.Boolean:
                    theme.Set(output, ConsoleThemeStyle.Boolean);
                    output.Write(token.ToStringValue());
                    theme.Reset(output);
                    break;
                case CsvToken.Null:
                    theme.Set(output, ConsoleThemeStyle.Null);
                    output.Write(token.ToStringValue());
                    theme.Reset(output);
                    break;
                case CsvToken.Number:
                    theme.Set(output, ConsoleThemeStyle.Number);
                    output.Write(token.ToStringValue());
                    theme.Reset(output);
                    break;
                case CsvToken.EscapedDoubleQuote:
                    theme.Set(output, ConsoleThemeStyle.Scalar);
                    output.Write(token.ToStringValue());
                    theme.Reset(output);
                    break;
                case CsvToken.Text:
                    theme.Set(output, isHeaderRow ? ConsoleThemeStyle.Name : ConsoleThemeStyle.Text);
                    output.Write(token.ToStringValue());
                    theme.Reset(output);
                    break;
                default:
                    throw new ArgumentException($"Unrecognized token `{token}`.");
            }
        }
    }
}