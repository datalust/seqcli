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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Seq.Forwarder.Config;
using Seq.Forwarder.Storage;
using Serilog;
using System.Threading.Tasks;
using Seq.Forwarder.Multiplexing;
using Seq.Forwarder.Util;

namespace Seq.Forwarder.Shipper
{
    sealed class HttpLogShipper : LogShipper
    {
        const string BulkUploadResource = "api/events/raw";

        readonly string? _apiKey;
        readonly LogBuffer _logBuffer;
        readonly SeqForwarderOutputConfig _outputConfig;
        readonly HttpClient _httpClient;
        readonly ExponentialBackoffConnectionSchedule _connectionSchedule;
        readonly ServerResponseProxy _serverResponseProxy;
        DateTime _nextRequiredLevelCheck;

        readonly object _stateLock = new object();
        readonly Timer _timer;
        bool _started;

        volatile bool _unloading;

        static readonly TimeSpan QuietWaitPeriod = TimeSpan.FromSeconds(2), MaximumConnectionInterval = TimeSpan.FromMinutes(2);

        public HttpLogShipper(LogBuffer logBuffer, string? apiKey, SeqForwarderOutputConfig outputConfig, ServerResponseProxy serverResponseProxy, HttpClient outputHttpClient)
        {
            _apiKey = apiKey;
            _httpClient = outputHttpClient ?? throw new ArgumentNullException(nameof(outputHttpClient));
            _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
            _outputConfig = outputConfig ?? throw new ArgumentNullException(nameof(outputConfig));
            _serverResponseProxy = serverResponseProxy ?? throw new ArgumentNullException(nameof(serverResponseProxy));
            _connectionSchedule = new ExponentialBackoffConnectionSchedule(QuietWaitPeriod);
            _timer = new Timer(s => OnTick());
        }

        public override void Start()
        {
            lock (_stateLock)
            {
                if (_started)
                    throw new InvalidOperationException("The shipper has already started.");

                if (_unloading)
                    throw new InvalidOperationException("The shipper is unloading.");

                Log.Information("Log shipper started, events will be dispatched to {ServerUrl}", _outputConfig.ServerUrl);

                _nextRequiredLevelCheck = DateTime.UtcNow.Add(MaximumConnectionInterval);
                _started = true;
                SetTimer();
            }
        }

        public override void Stop()
        {
            lock (_stateLock)
            {
                if (_unloading)
                    return;

                _unloading = true;

                if (!_started)
                    return;
            }

            var wh = new ManualResetEvent(false);
            if (_timer.Dispose(wh))
                wh.WaitOne();
        }
        
        public override void Dispose()
        {
            Stop();
        }

        void SetTimer()
        {
            _timer.Change(_connectionSchedule.NextInterval, Timeout.InfiniteTimeSpan);
        }

        void OnTick()
        {
            OnTickAsync().Wait();
        }

        async Task OnTickAsync()
        {
            try
            {
                var sendingSingles = 0;
                do
                {
                    var available = _logBuffer.Peek((int)_outputConfig.RawPayloadLimitBytes);
                    if (available.Length == 0)
                    {
                        if (DateTime.UtcNow < _nextRequiredLevelCheck || _connectionSchedule.LastConnectionFailed)
                        {
                            // For whatever reason, there's nothing waiting to send. This means we should try connecting again at the
                            // regular interval, so mark the attempt as successful.
                            _connectionSchedule.MarkSuccess();
                            break;
                        }
                    }

                    MakePayload(available, sendingSingles > 0, out Stream payload, out ulong lastIncluded);

                    var content = new StreamContent(new UnclosableStreamWrapper(payload));
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
                    {
                        CharSet = Encoding.UTF8.WebName
                    };

                    if (_apiKey != null)
                    {
                        content.Headers.Add(SeqApi.ApiKeyHeaderName, _apiKey);
                    }

                    var result = await _httpClient.PostAsync(BulkUploadResource, content);
                    if (result.IsSuccessStatusCode)
                    {
                        _connectionSchedule.MarkSuccess();
                        _logBuffer.Dequeue(lastIncluded);
                        if (sendingSingles > 0)
                            sendingSingles--;

                        _serverResponseProxy.SuccessResponseReturned(_apiKey, await result.Content.ReadAsStringAsync());
                        _nextRequiredLevelCheck = DateTime.UtcNow.Add(MaximumConnectionInterval);
                    }
                    else if (result.StatusCode == HttpStatusCode.BadRequest ||
                                result.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                    {
                        // The connection attempt was successful - the payload we sent was the problem.
                        _connectionSchedule.MarkSuccess();

                        if (sendingSingles != 0)
                        {
                            payload.Position = 0;
                            var payloadText = await new StreamReader(payload, Encoding.UTF8).ReadToEndAsync();
                            Log.Error("HTTP shipping failed with {StatusCode}: {Result}; payload was {InvalidPayload}", result.StatusCode, await result.Content.ReadAsStringAsync(), payloadText);
                            _logBuffer.Dequeue(lastIncluded);
                            sendingSingles = 0;
                        }
                        else
                        {
                            // Unscientific (should "binary search" in batches) but sending the next
                            // hundred events singly should flush out the problematic one.
                            sendingSingles = 100;
                        }
                    }
                    else
                    {
                        _connectionSchedule.MarkFailure();
                        Log.Error("Received failed HTTP shipping result {StatusCode}: {Result}", result.StatusCode, await result.Content.ReadAsStringAsync());
                        break;
                    }
                }
                while (true);
            }
            catch (HttpRequestException hex)
            {
                Log.Warning(hex, "HTTP request failed when sending a batch from the log shipper");
                _connectionSchedule.MarkFailure();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception while sending a batch from the log shipper");
                _connectionSchedule.MarkFailure();
            }
            finally
            {
                lock (_stateLock)
                {
                    if (!_unloading)
                        SetTimer();
                }
            }
        }

        void MakePayload(LogBufferEntry[] entries, bool oneOnly, out Stream utf8Payload, out ulong lastIncluded)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            lastIncluded = 0;

            var raw = new MemoryStream();
            var content = new StreamWriter(raw, Encoding.UTF8);
            content.Write("{\"Events\":[");
            content.Flush();
            var contentRemainingBytes = (int) _outputConfig.RawPayloadLimitBytes - 13; // Includes closing delims

            var delimStart = "";
            foreach (var logBufferEntry in entries)
            {
                if ((ulong)logBufferEntry.Value.Length > _outputConfig.EventBodyLimitBytes)
                {
                    Log.Information("Oversized event will be skipped, {Payload}", Encoding.UTF8.GetString(logBufferEntry.Value));
                    lastIncluded = logBufferEntry.Key;
                    continue;
                }

                // lastIncluded indicates we've added at least one event
                if (lastIncluded != 0 && contentRemainingBytes - (delimStart.Length + logBufferEntry.Value.Length) < 0)
                    break;

                content.Write(delimStart);
                content.Flush();
                contentRemainingBytes -= delimStart.Length;

                raw.Write(logBufferEntry.Value, 0, logBufferEntry.Value.Length);
                contentRemainingBytes -= logBufferEntry.Value.Length;

                lastIncluded = logBufferEntry.Key;

                delimStart = ",";
                if (oneOnly)
                    break;
            }

            content.Write("]}");
            content.Flush();
            raw.Position = 0;
            utf8Payload = raw;
        }
    }
}
