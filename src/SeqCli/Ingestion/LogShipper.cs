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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api;
using SeqCli.Api;
using SeqCli.Output;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;

namespace SeqCli.Ingestion;

static class LogShipper
{
    static readonly ITextFormatter JsonFormatter = OutputFormatter.Json(null);

    public static async Task ShipBufferAsync(
        SeqConnection connection,
        string? apiKey,
        ArraySegment<byte> utf8Clef,
        ILogger sendFailureLog,
        CancellationToken cancellationToken)
    {
        var content = new ByteArrayContent(utf8Clef.Array!, utf8Clef.Offset, utf8Clef.Count)
        {
            Headers =
            {
                ContentType = new MediaTypeHeaderValue(ApiConstants.ClefMediaType, "utf-8")
            }
        };
        
        var retries = 0;
        while (true)
        {
            try
            {
                var statusCode = await SendAsync(
                    connection,
                    apiKey,
                    sendFailureLog,
                    content,
                    cancellationToken);

                if ((int)statusCode is >= 200 and < 300)
                {
                    return;
                }

                if (statusCode == HttpStatusCode.BadRequest)
                {
                    sendFailureLog.Warning(
                        "Status code {StatusCode} indicates that the batch will not be accepted on retry; dropping",
                        (int)statusCode);
                    return;
                }
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sendFailureLog.Error(ex, "Failed to ship a batch");
            }
            
            var millisecondsDelay = (int)Math.Min(Math.Pow(2, retries) * 2000, 60000);
            sendFailureLog.Information("Backing off connection schedule; will retry in {MillisecondsDelay}", millisecondsDelay);
            await Task.Delay(millisecondsDelay, cancellationToken);
            retries += 1;
        }
    }
    
    public static async Task<int> ShipEventsAsync(
        SeqConnection connection,
        string? apiKey,
        ILogEventReader reader,
        InvalidDataHandling invalidDataHandling,
        SendFailureHandling sendFailureHandling,
        int batchSize,
        Func<LogEvent, bool>? filter,
        CancellationToken cancellationToken)
    {
        const int maxEmptyBatchWaitMS = 2000;
        var batch = await ReadBatchAsync(reader, filter, batchSize, invalidDataHandling, maxEmptyBatchWaitMS);
        var retries = 0;
        while (true)
        {
            var sendSucceeded = false;
            try
            {
                var statusCode = await SendBatchAsync(
                    connection,
                    apiKey,
                    batch.LogEvents,
                    sendFailureHandling != SendFailureHandling.Ignore ? Log.Logger : null,
                    cancellationToken);
                
                sendSucceeded = (int)statusCode is >= 200 and < 300;
            }
            catch (Exception ex)
            {
                if (sendFailureHandling != SendFailureHandling.Ignore)
                    Log.Error(ex, "Batch shipping failed");
            }

            if (!sendSucceeded)
            {
                if (sendFailureHandling == SendFailureHandling.Fail)
                    return 1;

                if (sendFailureHandling == SendFailureHandling.Retry)
                {
                    var millisecondsDelay = (int)Math.Min(Math.Pow(2, retries) * 2000, 60000);
                    await Task.Delay(millisecondsDelay);
                    retries += 1;
                    continue;
                }
            }

            retries = 0;

            if (batch.IsLast)
                break;
                
            batch = await ReadBatchAsync(reader, filter, batchSize, invalidDataHandling, maxEmptyBatchWaitMS);
        }

        return 0;
    }

    static async Task<BatchResult> ReadBatchAsync(
        ILogEventReader reader,
        Func<LogEvent, bool>? filter,
        int count,
        InvalidDataHandling invalidDataHandling,
        int maxWaitMS)
    {
        var batch = new List<LogEvent>();
        var isLast = false;
            
        // Avoid consuming stacks of CPU unnecessarily when there's no work to do. We do eventually yield
        // an empty batch, because level switching relies on this.
        var totalWaitMS = 0;
        const int idleWaitMS = 5;
        do
        {
            try
            {
                while (batch.Count < count)
                {
                    var rr = await reader.TryReadAsync();
                    isLast = rr.IsAtEnd;
                    var evt = rr.LogEvent;
                    if (evt == null)
                    {
                        if (isLast || batch.Count != 0 || totalWaitMS > maxWaitMS)
                            break;

                        // Nothing to to ship; wait to try to fill a batch.
                        await Task.Delay(idleWaitMS);
                        totalWaitMS += idleWaitMS;
                        continue;
                    }

                    if (filter == null || filter(evt))
                    {
                        batch.Add(evt);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is JsonReaderException || ex is InvalidDataException)
                {
                    if (invalidDataHandling == InvalidDataHandling.Ignore)
                        continue;
                }

                throw;
            }

            return new BatchResult(batch.ToArray(), isLast);
        } while (true);
    }

    static async Task<HttpStatusCode> SendBatchAsync(
        SeqConnection connection,
        string? apiKey,
        IReadOnlyCollection<LogEvent> batch,
        ILogger? sendFailureLog,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return HttpStatusCode.OK;

        StringContent content;
        // ReSharper disable once UseAwaitUsing
        using (var builder = new StringWriter())
        {
            foreach (var evt in batch)
                JsonFormatter.Format(evt, builder);

            content = new StringContent(builder.ToString(), Encoding.UTF8, ApiConstants.ClefMediaType);
        }

        return await SendAsync(connection, apiKey, sendFailureLog, content, cancellationToken);
    }

    static async Task<HttpStatusCode> SendAsync(SeqConnection connection, string? apiKey, ILogger? sendFailureLog, HttpContent content, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ApiConstants.IngestionEndpoint) { Content = content };
        if (apiKey != null)
            request.Headers.Add(ApiConstants.ApiKeyHeaderName, apiKey);

        var result = await connection.Client.HttpClient.SendAsync(request, cancellationToken);

        if (result.IsSuccessStatusCode || sendFailureLog == null)
            return result.StatusCode;

        var resultJson = await result.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(resultJson))
        {
            try
            {
                var error = JsonConvert.DeserializeObject<dynamic>(resultJson)!;

                sendFailureLog.Error("Shipping failed with status code {StatusCode}: {ErrorMessage}",
                    result.StatusCode,
                    (string)error.Error);

                return result.StatusCode;
            }
            catch
            {
                // ignored
            }
        }

        sendFailureLog.Error("Shipping failed with status code {StatusCode} ({ReasonPhrase})", result.StatusCode, result.ReasonPhrase);
        return result.StatusCode;
    }
}