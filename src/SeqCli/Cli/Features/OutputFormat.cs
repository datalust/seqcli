// Copyright © Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Seq.Api.Model;
using SeqCli.Csv;
using SeqCli.Output;
using SeqCli.Util;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Templates.Themes;

namespace SeqCli.Cli.Features;

sealed class OutputFormat(bool json, bool noColor, bool forceColor)
{
    public const string DefaultOutputTemplate =
        "[{Timestamp:o} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

    public static readonly ConsoleTheme DefaultAnsiTheme = AnsiConsoleTheme.Code;

    public static readonly ConsoleTheme DefaultTheme =
        OperatingSystem.IsWindows() ? SystemConsoleTheme.Literate : DefaultAnsiTheme;

    static readonly TemplateTheme DefaultTemplateTheme = Serilog.Templates.Themes.TemplateTheme.Code;

    public bool Json => json;

    bool ApplyThemeToRedirectedOutput => noColor == false && forceColor;

    ConsoleTheme Theme
        => noColor                     ? ConsoleTheme.None
            :  ApplyThemeToRedirectedOutput ? DefaultAnsiTheme
            :                                 DefaultTheme;

    TemplateTheme? TemplateTheme
        => noColor                     ? null
            :  ApplyThemeToRedirectedOutput ? DefaultTemplateTheme
            :                                 null;

    public Logger CreateOutputLogger()
    {
        var outputConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .Enrich.With<RedundantEventTypeRemovalEnricher>();

        if (json)
        {
            outputConfiguration.WriteTo.Console(OutputFormatter.Json(TemplateTheme));
        }
        else
        {
            outputConfiguration.WriteTo.Console(
                outputTemplate: DefaultOutputTemplate,
                theme: Theme,
                applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput);
        }

        return outputConfiguration.CreateLogger();
    }

    public void WriteCsv(string csv)
    {
        if (noColor )
        {
            Console.Write(csv);
        }
        else
        {
            var tokens = new CsvTokenizer().Tokenize(csv);
            CsvWriter.WriteCsv(tokens, Theme, Console.Out, true);
        }
    }

    public void WriteEntity(Entity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var jo = JObject.FromObject(
            entity,
            JsonSerializer.CreateDefault(new JsonSerializerSettings {
                DateParseHandling = DateParseHandling.None,
                Converters = {
                    new StringEnumConverter()
                }
            }));
            
        if (json)
        {
            jo.Remove("Links");
            // Proof-of-concept; this is a very inefficient
            // way to write colorized JSON ;)
                
            var writer = new LoggerConfiguration()
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Enrich.With<StripStructureTypeEnricher>()
                .WriteTo.Console(
                    outputTemplate: "{@Message:j}{NewLine}",
                    theme: Theme,
                    applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput)
                .CreateLogger();
            writer.Information("{@Entity}", jo);
        }
        else
        {
            var dyn = (dynamic) jo;
            Console.WriteLine($"{entity.Id} {dyn.Title ?? dyn.Name ?? dyn.Username ?? dyn.Expression}");
        }
    }

    public void WriteObject(object value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
            
        if (json)
        {
            var jo = JObject.FromObject(
                value,
                JsonSerializer.CreateDefault(new JsonSerializerSettings {
                    DateParseHandling = DateParseHandling.None,
                    Converters = {
                        new StringEnumConverter()
                    }
                }));

            // Using the same method of JSON colorization as above

            var writer = new LoggerConfiguration()
                .Destructure.With<JsonNetDestructuringPolicy>()
                .Enrich.With<StripStructureTypeEnricher>()
                .WriteTo.Console(
                    outputTemplate: "{@Message:j}{NewLine}",
                    theme: Theme,
                    applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput)
                .CreateLogger();
            writer.Information("{@Entity}", jo);
        }
        else
        {
            Console.WriteLine(value.ToString());
        }
    }

    public void ListEntities(IEnumerable<Entity> list)
    {
        foreach (var entity in list)
        {
            WriteEntity(entity);
        }
    }
    
    // ReSharper disable once MemberCanBeMadeStatic.Global
#pragma warning disable CA1822
    public void WriteText(string? text)
#pragma warning restore CA1822
    {
        Console.WriteLine(text?.TrimEnd());
    }
}
