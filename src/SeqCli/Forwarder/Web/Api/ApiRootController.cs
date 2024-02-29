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
using Microsoft.AspNetCore.Mvc;
using Seq.Forwarder.Diagnostics;
using SeqCli.Config;
using Serilog.Formatting.Display;

namespace Seq.Forwarder.Web.Api
{
    public class ApiRootController : Controller
    {
        static readonly Encoding Encoding = new UTF8Encoding(false);
        readonly MessageTemplateTextFormatter _ingestionLogFormatter;

        public ApiRootController(ForwarderDiagnosticConfig diagnosticConfig)
        {
            var template = "[{Timestamp:o} {Level:u3}] {Message}{NewLine}";
            if (diagnosticConfig.IngestionLogShowDetail)
                template += "Client IP address: {ClientHostIP}{NewLine}First {StartToLog} characters of payload: {DocumentStart:l}{NewLine}{Exception}{NewLine}";
            
            _ingestionLogFormatter = new MessageTemplateTextFormatter(template);
        }
        
        [HttpGet, Route("")]
        public IActionResult Index()
        {
            var events = IngestionLog.Read();
            using var log = new StringWriter();
            foreach (var logEvent in events)
            {
                _ingestionLogFormatter.Format(logEvent, log);
            }

            return Content(log.ToString(), "text/plain", Encoding);
        }

        [HttpGet, Route("api")]
        public IActionResult Resources()
        {
            return Content("{\"Links\":{\"Events\":\"/api/events/describe\"}}", "application/json", Encoding);
        }        
    }
}
