// Copyright 2018 Datalust Pty Ltd
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
using Destructurama;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Seq.Api.Model;
using SeqCli.Config;
using SeqCli.Csv;
using SeqCli.Levels;
using SeqCli.Output;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace SeqCli.Cli.Features
{
    class OutputFormatFeature : CommandFeature
    {
        public const string DefaultOutputTemplate =
            "[{Timestamp:o} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

        public static readonly ConsoleTheme DefaultTheme     = SystemConsoleTheme.Literate;
        public static readonly ConsoleTheme DefaultAnsiTheme = AnsiConsoleTheme.Literate;

        bool _json, _noColor, _forceColor;

        public OutputFormatFeature(SeqCliOutputConfig outputConfig)
        {
            _noColor = outputConfig.DisableColor;
            _forceColor = outputConfig.ForceColor;
        }

        public bool Json => _json;

        bool ApplyThemeToRedirectedOutput => _noColor == false && _forceColor == true;

        ConsoleTheme Theme
            => _noColor                     ? ConsoleTheme.None
            :  ApplyThemeToRedirectedOutput ? DefaultAnsiTheme
            :                                 DefaultTheme;

        public override void Enable(OptionSet options)
        {
            options.Add(
                "json",
                "Print output in newline-delimited JSON (the default is plain text)",
                v => _json = true);

            options.Add("no-color", "Don't colorize text output", v => _noColor = true);

            options.Add("c|force-color",
                "Force redirected output to have ANSI color (unless `--no-color` is also specified)",
                v => _forceColor = true);
        }

        public Logger CreateOutputLogger()
        {
            var outputConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LevelAlias.Minimum)
                .Enrich.With<RedundantEventTypeRemovalEnricher>();

            if (_json)
            {
                outputConfiguration.WriteTo.Console(new SurrogateLevelAwareCompactJsonFormatter());
            }
            else
            {
                outputConfiguration.Enrich.With<SurrogateLevelRemovalEnricher>();
                outputConfiguration.WriteTo.Console(
                    outputTemplate: DefaultOutputTemplate,
                    theme: Theme,
                    applyThemeToRedirectedOutput: ApplyThemeToRedirectedOutput);
            }

            return outputConfiguration.CreateLogger();
        }

        public void WriteCsv(string csv)
        {
            if (_noColor)
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
            
            if (_json)
            {
                jo.Remove("Links");
                // Proof-of-concept; this is a very inefficient
                // way to write colorized JSON ;)
                
                var writer = new LoggerConfiguration()
                    .Destructure.JsonNetTypes()
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
                Console.WriteLine($"{entity.Id} {dyn.Title ?? dyn.Name ?? dyn.Username}");
            }
        }
    }
}
