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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Seq.Api;
using SeqCli.Api;
using SeqCli.Levels;
using Serilog;
using Serilog.Events;

namespace SeqCli.Ingestion
{
    static class LogShipper
    {
        static readonly SurrogateLevelAwareCompactJsonFormatter Formatter = new SurrogateLevelAwareCompactJsonFormatter();

        public static async Task<int> ShipEvents(
            SeqConnection connection,
            string? apiKey,
            ILogEventReader reader,
            InvalidDataHandling invalidDataHandling,
            SendFailureHandling sendFailureHandling,
            int batchSize,
            Func<LogEvent, bool>? filter = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            const int maxEmptyBatchWaitMS = 2000;
            var batch = await ReadBatchAsync(reader, filter, batchSize, invalidDataHandling, maxEmptyBatchWaitMS);
            var retries = 0;
            while (true)
            {
                var sendSucceeded = false;
                try
                {
                    sendSucceeded = await SendBatchAsync(
                        connection,
                        apiKey,
                        batch.LogEvents,
                        sendFailureHandling != SendFailureHandling.Ignore);
                }
                catch (Exception ex)
                {
                    if (sendFailureHandling != SendFailureHandling.Ignore)
                        Log.Error(ex, "Failed to send an event batch");
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

        static async Task<bool> SendBatchAsync(
            SeqConnection connection,
            string? apiKey,
            IReadOnlyCollection<LogEvent> batch,
            bool logSendFailures)
        {
            if (batch.Count == 0)
                return true;

            StringContent content;
            // ReSharper disable once UseAwaitUsing
            using (var builder = new StringWriter())
            {
                foreach (var evt in batch)
                    Formatter.Format(evt, builder);

                content = new StringContent(builder.ToString(), Encoding.UTF8, ApiConstants.ClefMediatType);
            }

            var request = new HttpRequestMessage(HttpMethod.Post, ApiConstants.IngestionEndpoint) { Content = content };
            if (apiKey != null)
                request.Headers.Add(ApiConstants.ApiKeyHeaderName, apiKey);

            var result = await connection.Client.HttpClient.SendAsync(request);

            if (result.IsSuccessStatusCode)
                return true;

            if (!logSendFailures)
                return false;

            var resultJson = await result.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(resultJson))
            {
                try
                {
                    var error = JsonConvert.DeserializeObject<dynamic>(resultJson)!;

                    Log.Error("Failed with status code {StatusCode}: {ErrorMessage}",
                        result.StatusCode,
                        (string)error.Error);

                    return false;
                }
                catch
                {
                    // ignored
                }
            }

            Log.Error("Failed with status code {StatusCode} ({ReasonPhrase})", result.StatusCode, result.ReasonPhrase);
            return false;
        }
    }
}
