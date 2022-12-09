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
using System.Collections.Generic;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Ingestion;
using SeqCli.Levels;
using SeqCli.PlainText;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters.Expressions;

namespace SeqCli.Cli.Commands;

[Command("ingest", "Send log events from a file or `STDIN`",
    Example = "seqcli ingest -i log-*.txt --json --filter=\"@Level <> 'Debug'\" -p Environment=Test")]
class IngestCommand : Command
{
    const string DefaultPattern = "{@m:line}";
        
    readonly SeqConnectionFactory _connectionFactory;
    readonly InvalidDataHandlingFeature _invalidDataHandlingFeature;
    readonly FileInputFeature _fileInputFeature;
    readonly PropertiesFeature _properties;
    readonly SendFailureHandlingFeature _sendFailureHandlingFeature;
    readonly ConnectionFeature _connection;
    readonly BatchSizeFeature _batchSize;
    string? _filter, _level, _message;
    string _pattern = DefaultPattern;
    bool _json;

    public IngestCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _fileInputFeature = Enable(new FileInputFeature("File(s) to ingest", supportsWildcard: true));
        _invalidDataHandlingFeature = Enable<InvalidDataHandlingFeature>();
        _properties = Enable<PropertiesFeature>();

        Options.Add("x=|extract=",
            "An extraction pattern to apply to plain-text logs (ignored when `--json` is specified)",
            v => _pattern = string.IsNullOrWhiteSpace(v) ? DefaultPattern : v.Trim());

        Options.Add("json",
            "Read the events as JSON (the default assumes plain text)",
            v => _json = true);

        Options.Add("f=|filter=",
            "Filter expression to select a subset of events",
            v => _filter = string.IsNullOrWhiteSpace(v) ? null : v.Trim());

        Options.Add(
            "m=|message=",
            "A message to associate with the ingested events; https://messagetemplates.org syntax is supported",
            v => _message = string.IsNullOrWhiteSpace(v) ? null : v.Trim());

        Options.Add("l=|level=",
            "The level or severity to associate with the ingested events; this will override any " +
            "level information present in the events themselves",
            v => _level = string.IsNullOrWhiteSpace(v) ? null : v.Trim());

        _sendFailureHandlingFeature = Enable<SendFailureHandlingFeature>();            
        _connection = Enable<ConnectionFeature>();
        _batchSize = Enable<BatchSizeFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            var enrichers = new List<ILogEventEnricher>();
            foreach (var (name, value) in _properties.Properties)
                enrichers.Add(new ScalarPropertyEnricher(name, value));

            if (_level != null)
                enrichers.Add(new ScalarPropertyEnricher(SurrogateLevelProperty.PropertyName, _level));

            Func<LogEvent, bool>? filter = null;
            if (_filter != null)
            {
                var expr = _filter.Replace("@Level", SurrogateLevelProperty.PropertyName);
                var eval = FilterLanguage.CreateFilter(expr);
                filter = evt => true.Equals(eval(evt));
            }

            var connection = _connectionFactory.Connect(_connection);
            var (_, apiKey) = _connectionFactory.GetConnectionDetails(_connection);
            var batchSize = _batchSize.Value;

            foreach (var input in _fileInputFeature.OpenInputs())
            {
                using (input)
                {
                    var reader = _json
                        ? (ILogEventReader) new JsonLogEventReader(input)
                        : new PlainTextLogEventReader(input, _pattern);

                    reader = new EnrichingReader(reader, enrichers);

                    if (_message != null)
                        reader = new StaticMessageTemplateReader(reader, _message);

                    var exit = await LogShipper.ShipEvents(
                        connection,
                        apiKey,
                        reader,
                        _invalidDataHandlingFeature.InvalidDataHandling,
                        _sendFailureHandlingFeature.SendFailureHandling,
                        batchSize,
                        filter);

                    if (exit != 0)
                        return exit;
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ingestion failed: {ErrorMessage}", ex.Message);
            return 1;
        }
    }
}