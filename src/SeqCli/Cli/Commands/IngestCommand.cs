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
using System.IO;
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

namespace SeqCli.Cli.Commands
{
    [Command("ingest", "Send JSON log events from a file or `STDIN`",
        Example = "seqcli ingest -i events.clef --json --filter=\"@Level <> 'Debug'\" -p Environment=Test")]
    class IngestCommand : Command
    {
        const string DefaultPattern = "{@m:line}";
        
        readonly SeqConnectionFactory _connectionFactory;
        readonly InvalidDataHandlingFeature _invalidDataHandlingFeature;
        readonly FileInputFeature _fileInputFeature;
        readonly PropertiesFeature _properties;
        readonly SendFailureHandlingFeature _sendFailureHandlingFeature;
        readonly ConnectionFeature _connection;
        string _filter, _pattern = DefaultPattern;
        bool _json;

        public IngestCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _fileInputFeature = Enable(new FileInputFeature("CLEF file to ingest"));
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

            _sendFailureHandlingFeature = Enable<SendFailureHandlingFeature>();            
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            try
            {
                var enrichers = new List<ILogEventEnricher>();
                foreach (var property in _properties.Properties)
                    enrichers.Add(new ScalarPropertyEnricher(property.Key, property.Value));

                Func<LogEvent, bool> filter = null;
                if (_filter != null)
                {
                    var expr = _filter.Replace("@Level", SurrogateLevelProperty.PropertyName);
                    var eval = FilterLanguage.CreateFilter(expr);
                    filter = evt => true.Equals(eval(evt));
                }

                using (var input = _fileInputFeature.OpenInput())
                {
                    var reader = _json ?
                        (ILogEventReader)new JsonLogEventReader(input) :
                        new PlainTextLogEventReader(input, _pattern);
                    
                    return await LogShipper.ShipEvents(
                        _connectionFactory.Connect(_connection),
                        reader,
                        enrichers,
                        _invalidDataHandlingFeature.InvalidDataHandling,
                        _sendFailureHandlingFeature.SendFailureHandling,
                        filter);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Ingestion failed: {ErrorMessage}", ex.Message);
                return 1;
            }
        }
    }
}
