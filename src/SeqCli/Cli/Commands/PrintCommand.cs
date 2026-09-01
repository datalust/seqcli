// Copyright © Datalust and contributors.
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
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Ingestion;
using SeqCli.Output;
using SeqCli.Syntax;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands;

[Command("print", "Pretty-print events in CLEF/JSON format, from a file or `STDIN`",
    Example = "seqcli print -i log-20201028.clef")]
class PrintCommand : Command
{
    readonly FileInputFeature _fileInputFeature;
    readonly InvalidDataHandlingFeature _invalidDataHandlingFeature;
    readonly OutputFormatFeature _output;
    readonly StoragePathFeature _storage;

    string? _filter, _template;

    public PrintCommand()
    {
        _fileInputFeature = Enable(new FileInputFeature("CLEF file to read", allowMultiple: true));

        Options.Add("f=|filter=",
            "Filter expression to select a subset of events",
            v => _filter = ArgumentString.Normalize(v));

        Options.Add("template=",
            "Specify an output template to control plain text formatting",
            v => _template = ArgumentString.Normalize(v));

        _invalidDataHandlingFeature = Enable<InvalidDataHandlingFeature>();

        _output = Enable(new OutputFormatFeature(supportNative: false, supportJson: false));

        _storage = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storage);

        Func<JsonObject, bool>? filter = null;
        if (_filter != null)
        {
            if (!SeqSyntax.TryCompileExpression(_filter, out var compiled, out var error))
            {
                Log.Error("The specified filter could not be compiled: {Error}", error);
                return 1;
            }

            filter = evt => compiled(evt).IsTrue();
        }

        var template = _template == null ? null : PrintTemplate.InterpretEscapeChars(_template);
        var output = _output.GetOutputFormat(config, template);

        foreach (var input in _fileInputFeature.OpenInputs())
        {
            using (input)
            {
                var reader = new JsonEventReader(input);

                var isAtEnd = false;
                do
                {
                    try
                    {
                        var result = await reader.TryReadAsync();
                        isAtEnd = result.IsAtEnd;

                        if (result.Document != null && (filter == null || filter(result.Document)))
                            output.WriteEvent(result.Document);
                    }
                    catch (Exception ex)
                    {
                        if (ex is not JsonException && ex is not InvalidDataException ||
                            _invalidDataHandlingFeature.InvalidDataHandling != InvalidDataHandling.Ignore)
                            throw;
                    }
                } while (!isAtEnd);
            }
        }

        return 0;
    }
}