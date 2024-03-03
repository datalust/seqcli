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
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeqCli.Config;
using SeqCli.Forwarder.Diagnostics;
using SeqCli.Forwarder.Storage;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace SeqCli.Forwarder.Web.Api;

class IngestionEndpoints : IMapEndpoints
{
    static readonly Encoding Utf8 = new UTF8Encoding(false);

    readonly ConnectionConfig _connectionConfig;
    readonly LogBufferMap _logBuffers;

    readonly JsonSerializer _rawSerializer = JsonSerializer.Create(
        new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });

    public IngestionEndpoints(
        SeqCliConfig config,
        LogBufferMap logBuffers)
    {
        _connectionConfig = config.Connection;
        _logBuffers = logBuffers;
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
    
    IEnumerable<byte[]> EncodeRawEvents(ICollection<JToken> events, IPAddress remoteIpAddress)
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

    static string? ApiKey(HttpRequest request)
    {
        var apiKeyHeader = request.Headers["X-SeqApiKey"];

        if (apiKeyHeader.Count > 0) return apiKeyHeader.Last();
        if (request.Query.TryGetValue("apiKey", out var apiKey)) return apiKey.Last();

        return null;
    }
    
    
    IResult IngestRawFormat(HttpContext context)
    {
        // Convert legacy format to CLEF
        throw new NotImplementedException();
    }
    
    async Task<ContentHttpResult> IngestCompactFormat(HttpContext context)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        var log = _logBuffers.Get(ApiKey(context.Request));

        var payload = ArrayPool<byte>.Shared.Rent(1024 * 1024 * 10);
        var writeHead = 0;
        var readHead = 0;
        var discarding = false;

        var done = false;
        while (!done)
        {
            // Fill our buffer
            while (!done)
            {
                var remaining = payload.Length - writeHead;
                if (remaining == 0)
                {
                    break;
                }
                
                var read = await context.Request.Body.ReadAsync(payload.AsMemory(writeHead, remaining), context.RequestAborted);
                if (read == 0)
                {
                    done = true;
                }

                writeHead += read;
            }

            // Process events
            var batchStart = readHead;
            var batchEnd = readHead;
            while (batchEnd < writeHead)
            {
                var eventStart = batchEnd;
                var nlIndex = payload.AsSpan()[eventStart..].IndexOf((byte)'\n');
            
                if (nlIndex == -1)
                {
                    break;
                }

                var eventEnd = eventStart + nlIndex + 1;

                if (discarding)
                {
                    batchStart = eventEnd;
                    batchEnd = eventEnd;
                    readHead = batchEnd;

                    discarding = false;
                }
                else
                {
                    batchEnd = eventEnd;
                    readHead = batchEnd;

                    if (!ValidateClef(payload.AsSpan()[eventStart..batchEnd]))
                    {
                        await Write(log, ArrayPool<byte>.Shared, payload, batchStart..eventStart, cts.Token);
                        batchStart = batchEnd;
                    }
                }
            }

            if (batchStart != batchEnd)
            {
                await Write(log, ArrayPool<byte>.Shared, payload, batchStart..batchEnd, cts.Token);
            }
            else if (batchStart == 0)
            {
                readHead = payload.Length;
                discarding = true;
            }

            // Copy any unprocessed data into our buffer and continue
            if (!done)
            {
                var retain = payload.Length - readHead;
                payload.AsSpan()[retain..].CopyTo(payload.AsSpan()[..retain]);
                readHead = retain;
                writeHead = retain;
            }
        }
        
        // Exception cases are handled by `Write`
        ArrayPool<byte>.Shared.Return(payload);
        
        return TypedResults.Content(
            null, 
            "application/json", 
            Utf8, 
            StatusCodes.Status201Created);
    }

    bool ValidateClef(Span<byte> evt)
    {
        var reader = new Utf8JsonReader(evt);

        try
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                return false;
            }

            while (reader.Read())
            {
                if (reader.CurrentDepth == 1)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var name = reader.GetString();

                        if (name != null & name!.StartsWith("@"))
                        {
                            // Validate @ property
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return true;
    }

    async Task Write(LogBuffer log, ArrayPool<byte> pool, byte[] storage, Range range, CancellationToken cancellationToken)
    {
        try
        {
            await log.WriteAsync(storage, range, cancellationToken);
        }
        catch
        {
            pool.Return(storage);
            throw;
        }
    }
}