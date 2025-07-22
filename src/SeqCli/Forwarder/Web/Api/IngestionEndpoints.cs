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
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Seq.Api.Model.Shared;
using SeqCli.Api;
using SeqCli.Config;
using SeqCli.Forwarder.Channel;
using SeqCli.Forwarder.Diagnostics;
using JsonException = System.Text.Json.JsonException;

namespace SeqCli.Forwarder.Web.Api;

// ReSharper disable UnusedMethodReturnValue.Local

class IngestionEndpoints : IMapEndpoints
{
    static readonly Encoding Utf8 = new UTF8Encoding(false);

    readonly ForwardingChannelMap _forwardingChannels;
    readonly SeqCliConfig _config;

    public IngestionEndpoints(ForwardingChannelMap forwardingChannels, SeqCliConfig config)
    {
        _forwardingChannels = forwardingChannels;
        _config = config;
    }
    
    public void MapEndpoints(WebApplication app)
    {
        app.MapPost("ingest/clef", async context => await IngestCompactFormatAsync(context));
        app.MapPost("api/events/raw", async context => await IngestAsync(context));
    }

    async Task<IResult> IngestAsync(HttpContext context)
    {
        var clef = DefaultedBoolQuery(context.Request, "clef");

        if (clef) return await IngestCompactFormatAsync(context);

        var contentType = (string?)context.Request.Headers[HeaderNames.ContentType];
        const string clefMediaType = "application/vnd.serilog.clef";

        if (contentType != null && contentType.StartsWith(clefMediaType)) return await IngestCompactFormatAsync(context);

        IngestionLog.ForClient(context.Connection.RemoteIpAddress)
            .Error("Client supplied a legacy raw-format (non-CLEF) payload");
        return Error(HttpStatusCode.BadRequest, "Only newline-delimited JSON (CLEF) payloads are supported.");
    }
    
    async Task<IResult> IngestCompactFormatAsync(HttpContext context)
    {
        try
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            var log = _forwardingChannels.Get(GetApiKey(context.Request));
            
            var payload = ArrayPool<byte>.Shared.Rent(10 * 1024 * 1024);
            var writeHead = 0;
            var readHead = 0;
            
            var done = false;
            while (!done)
            {
                // Fill the memory buffer from as much of the incoming request payload as possible; buffering in memory increases the
                // size of write batches.
                while (!done)
                {
                    var remaining = payload.Length - writeHead;
                    if (remaining == 0)
                    {
                        break;
                    }
                    
                    var read = await context.Request.Body.ReadAsync(payload.AsMemory(writeHead, remaining), cts.Token);
                    if (read == 0)
                    {
                        done = true;
                    }
            
                    writeHead += read;
                }
                
                // Validate what we read, marking out a batch of one or more complete newline-delimited events.
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
            
                    batchEnd = eventEnd;
                    readHead = batchEnd;

                    if (!ValidateClef(payload.AsSpan()[eventStart..eventEnd], out var error))
                    {
                        var payloadText = Encoding.UTF8.GetString(payload.AsSpan()[eventStart..eventEnd]);
                        IngestionLog.ForPayload(context.Connection.RemoteIpAddress, payloadText)
                            .Error("Payload validation failed: {Error}", error);
                        return Error(HttpStatusCode.BadRequest, $"Payload validation failed: {error}.");
                    }
                }
            
                if (batchStart != batchEnd)
                {
                    await Write(log, ArrayPool<byte>.Shared, payload, batchStart..batchEnd, cts.Token);
                }
            
                // Copy any unprocessed data into our buffer and continue
                if (!done)
                {
                    var retain = writeHead - readHead;
                    payload.AsSpan()[readHead..writeHead].CopyTo(payload.AsSpan()[..retain]);
                    readHead = 0;
                    writeHead = retain;
                }
            }
            
            // Exception cases are handled by `Write`
            ArrayPool<byte>.Shared.Return(payload);
            
            return SuccessfulIngestion();
        }
        catch (Exception ex)
        {
            IngestionLog.ForClient(context.Connection.RemoteIpAddress)
                .Error(ex, "Ingestion failed");
            return Error(HttpStatusCode.InternalServerError, "Ingestion failed.");
        }
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

    static string? GetApiKey(HttpRequest request)
    {
        var apiKeyHeader = request.Headers[ApiConstants.ApiKeyHeaderName];

        if (apiKeyHeader.Count > 0) return apiKeyHeader.Last();
        return request.Query.TryGetValue("apiKey", out var apiKey) ? apiKey.Last() : null;
    }

    bool ValidateClef(Span<byte> evt, [NotNullWhen(false)] out string? errorFragment)
    {
        // Note that `errorFragment` does not include user-supplied values; we opt in to adding this to
        // the ingestion log and include it using `ForPayload()`.
        
        if (evt.Length > _config.Connection.EventSizeLimitBytes)
        {
            errorFragment = "an event exceeds the configured size limit";
            return false;
        }
        
        var reader = new Utf8JsonReader(evt);

        var foundTimestamp = false;
        try
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                errorFragment = $"unexpected token type `{reader.TokenType}`";
                return false;
            }

            while (reader.Read())
            {
                if (reader.CurrentDepth == 1)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var name = reader.GetString();

                        if (name == "@t")
                        {
                            if (!reader.Read())
                            {
                                errorFragment = "payload ended prematurely";
                                return false;
                            }
                            var value = reader.GetString();
                            if (!DateTimeOffset.TryParse(value, out _))
                            {
                                errorFragment = "unparseable `@t` timestamp value";
                                return false;
                            }

                            foundTimestamp = true;
                            break;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            errorFragment = "JSON parsing failure";
            return false;
        }

        if (!foundTimestamp)
        {
            errorFragment = "missing `@t` timestamp property";
            return false;
        }

        errorFragment = null;
        return true;
    }

    static async Task Write(ForwardingChannel forwardingChannel, ArrayPool<byte> pool, byte[] storage, Range range, CancellationToken cancellationToken)
    {
        try
        {
            await forwardingChannel.WriteAsync(storage, range, cancellationToken);
        }
        catch
        {
            pool.Return(storage);
            throw;
        }
    }
    
    static IResult Error(HttpStatusCode statusCode, string message)
    {
        return Results.Json(new ErrorPart { Error = message }, statusCode: (int)statusCode);
    }

    static IResult SuccessfulIngestion()
    {
        return TypedResults.Content(
            "{}",
            "application/json", 
            Utf8, 
            StatusCodes.Status201Created);
    }
}
