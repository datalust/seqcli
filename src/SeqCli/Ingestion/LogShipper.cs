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
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Compact.Reader;

namespace SeqCli.Ingestion
{
    static class LogShipper
    {
        // Keep things simple with a fixed batch size.
        const int BatchSize = 500;

        static readonly CompactJsonFormatter Formatter = new CompactJsonFormatter();

        public static async Task<int> ShipEvents(
            SeqConnection connection,
            LogEventReader reader,
            List<ILogEventEnricher> enrichers,
            InvalidDataHandling invalidDataHandling,
            Func<LogEvent, bool> filter = null)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (enrichers == null) throw new ArgumentNullException(nameof(enrichers));

            var batch = ReadBatch(reader, filter, BatchSize, invalidDataHandling);
            while (batch.Length > 0)
            {
                StringContent content;
                using (var builder = new StringWriter())
                {
                    foreach (var evt in batch)
                    {
                        foreach (var enricher in enrichers)
                            enricher.Enrich(evt, null);
                        Formatter.Format(evt, builder);
                    }

                    content = new StringContent(builder.ToString(), Encoding.UTF8, ApiConstants.ClefMediatType);
                }

                var result = await connection.Client.HttpClient.PostAsync(ApiConstants.IngestionEndpoint, content);

                if (result.IsSuccessStatusCode)
                {
                    batch = ReadBatch(reader, filter, BatchSize, invalidDataHandling);
                    continue;
                }

                var resultJson = await result.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(resultJson))
                {
                    try
                    {
                        var error = JsonConvert.DeserializeObject<dynamic>(resultJson);

                        Log.Error("Failed with status code {StatusCode}: {ErrorMessage}",
                            result.StatusCode,
                            (string)error.ErrorMessage);
                    }
                    catch
                    {
                        // ignored
                    }
                }

                Log.Error("Failed with status code {StatusCode} ({ReasonPhrase})", result.StatusCode, result.ReasonPhrase);
                return 1;
            }

            return 0;
        }

        static LogEvent[] ReadBatch(LogEventReader reader, Func<LogEvent, bool> filter,
            int count, InvalidDataHandling invalidDataHandling)
        {
            var batch = new List<LogEvent>();
            do
            {
                try
                {
                    while (batch.Count < count && reader.TryRead(out var evt))
                    {
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

                return batch.ToArray();
            } while (true);
        }
    }
}
