﻿// Copyright 2018 Datalust Pty Ltd
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
using System.Threading;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Ingestion;
using SeqCli.Levels;
using SeqCli.PlainText;
using SeqCli.Syntax;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Cli.Commands;

[Command("ingest", "Send log events from a file or `STDIN`",
    Example = "seqcli ingest -i log-*.txt --json --filter=\"@Level <> 'Debug'\" -p Environment=Test")]
class IngestCommand : Command
{
    const string DefaultPattern = "{@m:line}";
        
    readonly InvalidDataHandlingFeature _invalidDataHandlingFeature;
    readonly FileInputFeature _fileInputFeature;
    readonly PropertiesFeature _properties;
    readonly SendFailureHandlingFeature _sendFailureHandlingFeature;
    readonly ConnectionFeature _connection;
    readonly BatchSizeFeature _batchSize;
    readonly StoragePathFeature _storagePath;
    string? _filter, _level, _message;
    string _pattern = DefaultPattern;
    bool _json;

    public IngestCommand()
    {
        _fileInputFeature = Enable(new FileInputFeature("File(s) to ingest", allowMultiple: true));
        _invalidDataHandlingFeature = Enable<InvalidDataHandlingFeature>();
        _properties = Enable<PropertiesFeature>();

        Options.Add("x=|extract=",
            "An extraction pattern to apply to plain-text logs (ignored when `--json` is specified)",
            v => _pattern = string.IsNullOrWhiteSpace(v) ? DefaultPattern : v.Trim());

        Options.Add("json",
            "Read the events as JSON (the default assumes plain text)",
            _ => _json = true);

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
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        try
        {
            var enrichers = new List<ILogEventEnricher>();
            
            if (_level != null)
                enrichers.Add(new ScalarPropertyEnricher(LevelMapping.SurrogateLevelProperty, _level));
            
            foreach (var (name, value) in _properties.FlatProperties)
                enrichers.Add(new ScalarPropertyEnricher(name, value));

            Func<LogEvent, bool>? filter = null;
            if (_filter != null)
            {
                var eval = SeqSyntax.CompileExpression(_filter);
                filter = evt => Seq.Syntax.Expressions.ExpressionResult.IsTrue(eval(evt));
            }

            var config = RuntimeConfigurationLoader.Load(_storagePath);
            var connection = SeqConnectionFactory.Connect(_connection, config);
            
            // The API key is passed through separately because `SeqConnection` doesn't expose a batched ingestion
            // mechanism and so we manually construct `HttpRequestMessage`s deeper in the stack. Nice feature gap to
            // close at some point!
            var (_, apiKey) = SeqConnectionFactory.GetConnectionDetails(_connection, config);
            var batchSize = _batchSize.Value;

            foreach (var input in _fileInputFeature.OpenInputs())
            {
                using (input)
                {
                    ILogEventReader reader = _json
                        ? new JsonLogEventReader(input)
                        : new PlainTextLogEventReader(input, _pattern);

                    reader = new EnrichingReader(reader, enrichers);

                    if (_message != null)
                        reader = new StaticMessageTemplateReader(reader, _message);

                    var exit = await LogShipper.ShipEventsAsync(
                        connection,
                        apiKey,
                        reader,
                        _invalidDataHandlingFeature.InvalidDataHandling,
                        _sendFailureHandlingFeature.SendFailureHandling,
                        batchSize,
                        filter,
                        CancellationToken.None);

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