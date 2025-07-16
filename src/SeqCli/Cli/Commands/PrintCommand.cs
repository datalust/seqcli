// Copyright 2019 Datalust Pty Ltd and Contributors
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
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Syntax.Expressions;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Ingestion;
using SeqCli.Output;
using SeqCli.Util;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace SeqCli.Cli.Commands;

[Command("print", "Pretty-print events in CLEF/JSON format, from a file or `STDIN`",
    Example = "seqcli print -i log-20201028.clef")]
class PrintCommand : Command
{
    readonly FileInputFeature _fileInputFeature;
    readonly InvalidDataHandlingFeature _invalidDataHandlingFeature;

    string? _filter, _template = OutputFormatFeature.DefaultOutputTemplate;
    bool _noColor, _forceColor;

    public PrintCommand(SeqCliOutputConfig seqCliOutputConfig)
    {
        if (seqCliOutputConfig == null) throw new ArgumentNullException(nameof(seqCliOutputConfig));
        _noColor = seqCliOutputConfig.DisableColor;
        _forceColor = seqCliOutputConfig.ForceColor;

        _fileInputFeature = Enable(new FileInputFeature("CLEF file to read", allowMultiple: true));

        Options.Add("f=|filter=",
            "Filter expression to select a subset of events",
            v => _filter = ArgumentString.Normalize(v));

        Options.Add("template=",
            "Specify an output template to control plain text formatting",
            v => _template = ArgumentString.Normalize(v));

        _invalidDataHandlingFeature = Enable<InvalidDataHandlingFeature>();

        Options.Add("no-color", "Don't colorize text output", _ => _noColor = true);

        Options.Add("force-color",
            "Force redirected output to have ANSI color (unless `--no-color` is also specified)",
            _ => _forceColor = true);
    }

    protected override async Task<int> Run()
    {
        var applyThemeToRedirectedOutput
            = !_noColor && _forceColor;

        var theme
            = _noColor                      ? ConsoleTheme.None
            :  applyThemeToRedirectedOutput ? OutputFormatFeature.DefaultAnsiTheme
            :                                 OutputFormatFeature.DefaultTheme;

        var outputConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(LevelAlias.Minimum)
            .Enrich.With<RedundantEventTypeRemovalEnricher>()
            .WriteTo.Console(
                outputTemplate: _template ?? OutputFormatFeature.DefaultOutputTemplate,
                theme: theme,
                applyThemeToRedirectedOutput: applyThemeToRedirectedOutput);

        if (_filter != null)
        {
            if (!SerilogExpression.TryCompile(_filter, out var filter, out var error))
            {
                Log.Error("The specified filter could not be compiled: {Error}", error);
                return 1;
            }
            
            outputConfiguration.Filter.ByIncludingOnly(evt => ExpressionResult.IsTrue(filter(evt)));
        }

        await using var logger = outputConfiguration.CreateLogger();
        foreach (var input in _fileInputFeature.OpenInputs())
        {
            using (input)
            {
                var reader = new JsonLogEventReader(input);

                var isAtEnd = false;
                do
                {
                    try
                    {
                        var result = await reader.TryReadAsync();
                        isAtEnd = result.IsAtEnd;

                        if (result.LogEvent != null)
                            logger.Write(result.LogEvent);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not JsonReaderException && ex is not InvalidDataException ||
                            _invalidDataHandlingFeature.InvalidDataHandling != InvalidDataHandling.Ignore)
                            throw;
                    }
                } while (!isAtEnd);
            }
        }

        return 0;
    }
}