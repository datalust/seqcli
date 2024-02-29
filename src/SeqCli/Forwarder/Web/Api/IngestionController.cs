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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Forwarder.Diagnostics;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Schema;
using Seq.Forwarder.Shipper;
using SeqCli.Config;

namespace Seq.Forwarder.Web.Api
{
    public class IngestionController : Controller
    {
        static readonly Encoding Encoding = new UTF8Encoding(false);
        const string ClefMediaType = "application/vnd.serilog.clef";

        readonly ActiveLogBufferMap _logBufferMap;
        readonly ConnectionConfig _outputConfig;
        readonly ServerResponseProxy _serverResponseProxy;

        readonly JsonSerializer _rawSerializer = JsonSerializer.Create(
            new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

        public IngestionController(ActiveLogBufferMap logBufferMap, ConnectionConfig outputConfig, ServerResponseProxy serverResponseProxy)
        {
            _logBufferMap = logBufferMap;
            _outputConfig = outputConfig;
            _serverResponseProxy = serverResponseProxy;
        }

        IPAddress ClientHostIP => Request.HttpContext.Connection.RemoteIpAddress!;

        [HttpGet, Route("api/events/describe")]
        public IActionResult Resources()
        {
            return Content("{\"Links\":{\"Raw\":\"/api/events/raw{?clef}\"}}", "application/json", Encoding);
        }

        [HttpPost, Route("api/events/raw")]
        public async Task<IActionResult> Ingest()
        {
            var clef = DefaultedBoolQuery("clef");

            if (clef)
                return await IngestCompactFormat();

            var contentType = (string?) Request.Headers[HeaderNames.ContentType];
            if (contentType != null && contentType.StartsWith(ClefMediaType))
                return await IngestCompactFormat();

            return IngestRawFormat();
        }

        IActionResult IngestRawFormat()
        {
            // The compact format ingestion path works with async IO.
            HttpContext.Features.Get<IHttpBodyControlFeature>()!.AllowSynchronousIO = true;
            
            JObject posted;
            try
            {
                posted = _rawSerializer.Deserialize<JObject>(new JsonTextReader(new StreamReader(Request.Body))) ??
                         throw new RequestProcessingException("Request body payload is JSON `null`.");
            }
            catch (Exception ex)
            {
                IngestionLog.ForClient(ClientHostIP).Debug(ex,"Rejecting payload due to invalid JSON, request body could not be parsed");
                throw new RequestProcessingException("Invalid raw event JSON, body could not be parsed.");
            }

            if (!(posted.TryGetValue("events", StringComparison.Ordinal, out var eventsToken) ||
                  posted.TryGetValue("Events", StringComparison.Ordinal, out eventsToken)))
            {
                IngestionLog.ForClient(ClientHostIP).Debug("Rejecting payload due to invalid JSON structure");
                throw new RequestProcessingException("Invalid raw event JSON, body must contain an 'Events' array.");
            }

            if (!(eventsToken is JArray events))
            {
                IngestionLog.ForClient(ClientHostIP).Debug("Rejecting payload due to invalid Events property structure");
                throw new RequestProcessingException("Invalid raw event JSON, the 'Events' property must be an array.");
            }

            var encoded = EncodeRawEvents(events);
            return Enqueue(encoded);
        }

        async Task<IActionResult> IngestCompactFormat()
        {
            var rawFormat = new List<JToken>();
            var reader = new StreamReader(Request.Body);

            var line = await reader.ReadLineAsync();
            var lineNumber = 1;

            while (line != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    JObject item;
                    try
                    {
                        item = _rawSerializer.Deserialize<JObject>(new JsonTextReader(new StringReader(line))) ??
                               throw new RequestProcessingException("Request body payload is JSON `null`.");
                    }
                    catch (Exception ex)
                    {
                        IngestionLog.ForPayload(ClientHostIP, line).Debug(ex, "Rejecting CLEF payload due to invalid JSON, item could not be parsed");
                        throw new RequestProcessingException($"Invalid raw event JSON, item on line {lineNumber} could not be parsed.");
                    }

                    if (!EventSchema.FromClefFormat(lineNumber, item, out var evt, out var err))
                    {
                        IngestionLog.ForPayload(ClientHostIP, line).Debug("Rejecting CLEF payload due to invalid event JSON structure: {NormalizationError}", err);
                        throw new RequestProcessingException(err);
                    }

                    rawFormat.Add(evt);
                }

                line = await reader.ReadLineAsync();
                ++lineNumber;
            }

            var encoded = EncodeRawEvents(rawFormat);
            return Enqueue(encoded);
        }

        byte[][] EncodeRawEvents(ICollection<JToken> events)
        {
            var encoded = new byte[events.Count][];
            var i = 0;
            foreach (var e in events)
            {
                var s = e.ToString(Formatting.None);
                var payload = Encoding.UTF8.GetBytes(s);

                if (payload.Length > (int) _outputConfig.EventBodyLimitBytes)
                {
                    IngestionLog.ForPayload(ClientHostIP, s).Debug("An oversized event was dropped");

                    var jo = e as JObject;
                    // ReSharper disable SuspiciousTypeConversion.Global
                    var timestamp = (string?) (dynamic?) jo?.GetValue("Timestamp") ?? DateTime.UtcNow.ToString("o");
                    var level = (string?) (dynamic?) jo?.GetValue("Level") ?? "Warning";

                    if (jo != null)
                    {
                        jo.Remove("Timestamp");
                        jo.Remove("Level");
                    }

                    var startToLog = (int) Math.Min(_outputConfig.EventBodyLimitBytes / 2, 1024);
                    var compactPrefix = e.ToString(Formatting.None).Substring(0, startToLog);

                    encoded[i] = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
                    {
                        Timestamp = timestamp,
                        MessageTemplate = "Seq Forwarder received and dropped an oversized event",
                        Level = level,
                        Properties = new
                        {
                            Partial = compactPrefix,
                            Environment.MachineName,
                            _outputConfig.EventBodyLimitBytes,
                            PayloadBytes = payload.Length
                        }
                    }));
                }
                else
                {
                    encoded[i] = payload;
                }

                i++;
            }

            return encoded;
        }
        
        IActionResult Enqueue(byte[][] encodedEvents)
        {
            var apiKey = GetRequestApiKeyToken();
            _logBufferMap.GetLogBuffer(apiKey).Enqueue(encodedEvents);
            
            var response = Content(_serverResponseProxy.GetResponseText(apiKey), "application/json", Encoding);
            response.StatusCode = (int)HttpStatusCode.Created;
            return response;
        }

        string? GetRequestApiKeyToken()
        {
            var apiKeyToken = Request.Headers[SeqApi.ApiKeyHeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(apiKeyToken))
                apiKeyToken = Request.Query["apiKey"];

            var normalized = apiKeyToken?.Trim();
            if (string.IsNullOrEmpty(normalized))
                return null;

            return normalized;
        }
        
        bool DefaultedBoolQuery(string queryParameterName)
        {
            var parameter = Request.Query[queryParameterName];
            if (parameter.Count != 1)
                return false;

            var value = (string?) parameter;

            if (value == "" && (
                Request.QueryString.Value!.Contains($"&{queryParameterName}=") ||
                Request.QueryString.Value.Contains($"?{queryParameterName}=")))
            {
                return false;
            }

            return "true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "" || value == queryParameterName;
        }
    }
}
