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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeqCli.Config;
using SeqCli.Forwarder.Diagnostics;
using SeqCli.Forwarder.Multiplexing;
using SeqCli.Forwarder.Schema;
using SeqCli.Forwarder.Shipper;

namespace SeqCli.Forwarder.Web.Api;

class IngestionEndpoints : IMapEndpoints
{
    static readonly Encoding Utf8 = new UTF8Encoding(false);

    readonly ActiveLogBufferMap _logBufferMap;
    readonly ConnectionConfig _connectionConfig;
    readonly ServerResponseProxy _serverResponseProxy;

    readonly JsonSerializer _rawSerializer = JsonSerializer.Create(
        new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

    public IngestionEndpoints(
        ActiveLogBufferMap logBufferMap,
        ServerResponseProxy serverResponseProxy, 
        ConnectionConfig connectionConfig)
    {
        _logBufferMap = logBufferMap;
        _connectionConfig = connectionConfig;
        _serverResponseProxy = serverResponseProxy;
    }
    
    public void Map(WebApplication app)
    {
        app.MapGet("/api",
            () => Results.Content("{\"Links\":{\"Events\":\"/api/events/describe\"}}", "application/json", Utf8));

        app.MapPost("api/events/raw", new Func<HttpContext, Task<IResult>>(async (context) =>
        {
            var clef = DefaultedBoolQuery(context.Request, "clef");

            if (clef)
                return await IngestCompactFormat(context);

            var contentType = (string?) context.Request.Headers[HeaderNames.ContentType];
            var clefMediaType = "application/vnd.serilog.clef";

            if (contentType != null && contentType.StartsWith(clefMediaType))
                return await IngestCompactFormat(context);

            return IngestRawFormat(context);
        }));
    }
    
    byte[][] EncodeRawEvents(ICollection<JToken> events, IPAddress remoteIpAddress)
    {
        var encoded = new byte[events.Count][];
        var i = 0;
        foreach (var e in events)
        {
            var s = e.ToString(Formatting.None);
            var payload = Utf8.GetBytes(s);

            if (payload.Length > (int) _connectionConfig.EventBodyLimitBytes)
            {
                IngestionLog.ForPayload(remoteIpAddress, s).Debug("An oversized event was dropped");

                var jo = e as JObject;
                // ReSharper disable SuspiciousTypeConversion.Global
                var timestamp = (string?) (dynamic?) jo?.GetValue("Timestamp") ?? DateTime.UtcNow.ToString("o");
                var level = (string?) (dynamic?) jo?.GetValue("Level") ?? "Warning";

                if (jo != null)
                {
                    jo.Remove("Timestamp");
                    jo.Remove("Level");
                }

                var startToLog = (int) Math.Min(_connectionConfig.EventBodyLimitBytes / 2, 1024);
                var compactPrefix = e.ToString(Formatting.None).Substring(0, startToLog);

                encoded[i] = Utf8.GetBytes(JsonConvert.SerializeObject(new
                {
                    Timestamp = timestamp,
                    MessageTemplate = "Seq Forwarder received and dropped an oversized event",
                    Level = level,
                    Properties = new
                    {
                        Partial = compactPrefix,
                        Environment.MachineName,
                        _connectionConfig.EventBodyLimitBytes,
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
    
    static bool DefaultedBoolQuery(HttpRequest request, string queryParameterName)
    {
        var parameter = request.Query[queryParameterName];
        if (parameter.Count != 1)
            return false;

        var value = (string?) parameter;

        if (value == "" && (
                request.QueryString.Value!.Contains($"&{queryParameterName}=") ||
                request.QueryString.Value.Contains($"?{queryParameterName}=")))
        {
            return false;
        }

        return "true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "" || value == queryParameterName;
    }
    
    IResult IngestRawFormat(HttpContext context)
    {
        // The compact format ingestion path works with async IO.
        context.Features.Get<IHttpBodyControlFeature>()!.AllowSynchronousIO = true;

        JObject posted;
        try
        {
            posted = _rawSerializer.Deserialize<JObject>(new JsonTextReader(new StreamReader(context.Request.Body))) ??
                     throw new RequestProcessingException("Request body payload is JSON `null`.");
        }
        catch (Exception ex)
        {
            IngestionLog.ForClient(context.Connection.RemoteIpAddress!).Debug(ex,"Rejecting payload due to invalid JSON, request body could not be parsed");
            throw new RequestProcessingException("Invalid raw event JSON, body could not be parsed.");
        }

        if (!(posted.TryGetValue("events", StringComparison.Ordinal, out var eventsToken) ||
              posted.TryGetValue("Events", StringComparison.Ordinal, out eventsToken)))
        {
            IngestionLog.ForClient(context.Connection.RemoteIpAddress!).Debug("Rejecting payload due to invalid JSON structure");
            throw new RequestProcessingException("Invalid raw event JSON, body must contain an 'Events' array.");
        }

        if (!(eventsToken is JArray events))
        {
            IngestionLog.ForClient(context.Connection.RemoteIpAddress!).Debug("Rejecting payload due to invalid Events property structure");
            throw new RequestProcessingException("Invalid raw event JSON, the 'Events' property must be an array.");
        }

        var encoded = EncodeRawEvents(events, context.Connection.RemoteIpAddress!);
        return Enqueue(context.Request, encoded);
    }
    
    async Task<ContentHttpResult> IngestCompactFormat(HttpContext context)
    {
        var rawFormat = new List<JToken>();
        var reader = new StreamReader(context.Request.Body);

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
                    IngestionLog.ForPayload(context.Connection.RemoteIpAddress!, line).Debug(ex, "Rejecting CLEF payload due to invalid JSON, item could not be parsed");
                    throw new RequestProcessingException($"Invalid raw event JSON, item on line {lineNumber} could not be parsed.");
                }

                if (!EventSchema.FromClefFormat(lineNumber, item, out var evt, out var err))
                {
                    IngestionLog.ForPayload(context.Connection.RemoteIpAddress!, line).Debug("Rejecting CLEF payload due to invalid event JSON structure: {NormalizationError}", err);
                    throw new RequestProcessingException(err);
                }

                rawFormat.Add(evt);
            }

            line = await reader.ReadLineAsync();
            ++lineNumber;
        }

        return Enqueue(
            context.Request, 
            EncodeRawEvents(rawFormat, context.Connection.RemoteIpAddress!));
    }
    
    ContentHttpResult Enqueue(HttpRequest request, byte[][] encodedEvents)
    {
        var apiKeyToken = request.Headers[SeqApi.ApiKeyHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(apiKeyToken))
            apiKeyToken = request.Query["apiKey"];

        var apiKey = string.IsNullOrWhiteSpace(apiKeyToken) 
            ? null 
            : apiKeyToken.Trim();
        
        _logBufferMap.GetLogBuffer(apiKey).Enqueue(encodedEvents);

        return TypedResults.Content(
            _serverResponseProxy.GetResponseText(apiKey), 
            "application/json", 
            Utf8, 
            StatusCodes.Status201Created);
        
    }
}