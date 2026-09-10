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
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SeqCli.Forwarder.Diagnostics;
using Serilog.Events;

namespace SeqCli.Forwarder.Web.Api;

class IngestionLogEndpoints : IMapEndpoints
{
    readonly bool _showDetail;
    readonly Encoding _utf8 = new UTF8Encoding(false);

    public IngestionLogEndpoints(bool showDetail)
    {
        _showDetail = showDetail;
    }

    public void MapEndpoints(WebApplication app)
    {
        app.MapGet("/api",
            () => Results.Content("{\"Links\":{\"DiagnosticsResources\":\"/api/diagnostics/resources\"}}", "application/json", _utf8));
        
        app.MapGet("/api/diagnostics/resources",
            () => Results.Content("{\"Links\":{\"Self\":\"api/diagnostics/resources\",\"IngestionLog\":\"api/diagnostics/ingestion\"}}", "application/json", _utf8));

        app.MapGet("api/diagnostics/ingestion", () =>
        {
            var events = IngestionLog.Read();
            using var log = new StringWriter();
            foreach (var logEvent in events)
            {
                Format(logEvent, log);
            }

            return Results.Content(log.ToString(), "text/plain", _utf8);
        });
    }

    void Format(LogEvent logEvent, TextWriter log)
    {
        log.Write($"[{logEvent.Timestamp:o} {Abbreviate(logEvent.Level)}] ");

        static string Abbreviate(LogEventLevel logEventLevel)
        {
            // Here because we don't want Serilog level conversion routines, or any other Serilog model conversion
            // routines, to propagate.
            return logEventLevel switch
            {
                LogEventLevel.Verbose => "VRB",
                LogEventLevel.Debug => "DBG",
                LogEventLevel.Information => "INF",
                LogEventLevel.Warning => "WAR",
                LogEventLevel.Error => "ERR",
                LogEventLevel.Fatal => "FTL",
                _ => throw new ArgumentOutOfRangeException(nameof(logEventLevel), logEventLevel, null)
            };
        }

        logEvent.RenderMessage(log, CultureInfo.InvariantCulture);
        log.WriteLine();
        if (_showDetail)
        {
            if (logEvent.Properties.TryGetValue("ClientHostIP", out var clientHostIPProperty) &&
                clientHostIPProperty is ScalarValue { Value: string clientHostIP})
            {
                log.WriteLine($"Client IP address: {clientHostIP}");
            }
            
            if (logEvent.Properties.TryGetValue("DocumentStart", out var documentStartProperty) &&
                documentStartProperty is ScalarValue { Value: string documentStart} &&
                logEvent.Properties.TryGetValue("StartToLog", out var startToLogProperty) &&
                startToLogProperty is ScalarValue { Value: {} startToLog })
            {
                log.WriteLine($"First {startToLog} characters of payload: {documentStart}");
            }

            if (logEvent.Exception is { } exception)
            {
                log.WriteLine(exception);
            }
        }
    }
}
