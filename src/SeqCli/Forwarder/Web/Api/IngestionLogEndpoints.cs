﻿// Copyright © Datalust Pty Ltd
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
using SeqCli.Forwarder.Diagnostics;
using Serilog.Formatting.Display;

namespace SeqCli.Forwarder.Web.Api;

class IngestionLogEndpoints : IMapEndpoints
{
    readonly MessageTemplateTextFormatter _formatter;
    readonly Encoding _utf8 = new UTF8Encoding(false);

    public IngestionLogEndpoints(MessageTemplateTextFormatter formatter)
    {
        _formatter = formatter;
    }

    public void MapEndpoints(WebApplication app)
    {
        // ISSUE: this route should probably only be mapped when some kind of --unsafe-debug flag
        // is set.
        
        app.MapGet("/", () =>
        {
            var events = IngestionLog.Read();
            using var log = new StringWriter();
            foreach (var logEvent in events)
            {
                _formatter.Format(logEvent, log);
            }

            return Results.Content(log.ToString(), "text/plain", _utf8);
        });
    }
}