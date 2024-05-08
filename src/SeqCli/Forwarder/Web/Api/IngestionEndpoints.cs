﻿// Copyright Datalust Pty Ltd
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
    
    public void MapEndpoints(WebApplication app)
    {
        app.MapPost("api/events/raw", new Func<HttpContext, Task<IResult>>(async (context) =>
        {
            var clef = DefaultedBoolQuery(context.Request, "clef");

            if (clef)
                return await IngestCompactFormat(context);

            var contentType = (string?) context.Request.Headers[HeaderNames.ContentType];
            const string clefMediaType = "application/vnd.serilog.clef";

            if (contentType != null && contentType.StartsWith(clefMediaType))
                return await IngestCompactFormat(context);

            IngestionLog.ForClient(context.Connection.RemoteIpAddress)
                .Error("Client supplied a legacy raw-format (non-CLEF) payload");
            return Results.BadRequest("Only newline-delimited JSON (CLEF) payloads are supported.");
        }));
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
        return request.Query.TryGetValue("apiKey", out var apiKey) ? apiKey.Last() : null;
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

    static bool ValidateClef(Span<byte> evt)
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

                        if (name != null & name!.StartsWith($"@"))
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

    static async Task Write(LogBuffer log, ArrayPool<byte> pool, byte[] storage, Range range, CancellationToken cancellationToken)
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