// Copyright Datalust Pty Ltd
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

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SeqCli.Config.Forwarder;
using SeqCli.Forwarder.Diagnostics;
using Serilog.Formatting.Display;

namespace SeqCli.Forwarder.Web.Api;

public class ApiRoot
{    
    static readonly Encoding Encoding = new UTF8Encoding(false);

    public static void Map(WebApplication app, MessageTemplateTextFormatter formatter)
    {
        app.MapGet("/", () =>
        {
            var events = IngestionLog.Read();
            using var log = new StringWriter();
            foreach (var logEvent in events)
            {
                formatter.Format(logEvent, log);
            }

            return Results.Content(log.ToString(), "text/plain", Encoding);
        });

        app.MapGet("/api",
            () => Results.Content("{\"Links\":{\"Events\":\"/api/events/describe\"}}", "application/json", Encoding));

    }
}