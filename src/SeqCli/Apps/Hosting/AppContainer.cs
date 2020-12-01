// Copyright Datalust Pty Ltd and Contributors
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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;
using SeqCli.Levels;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact.Reader;

// ReSharper disable IdentifierTypo, StringLiteralTypo, SuspiciousTypeConversion.Global

namespace SeqCli.Apps.Hosting
{
    class AppContainer : IAppHost, IDisposable
    {
        readonly SeqApp _seqApp;
        readonly AppLoader _loader;

        readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Culture = CultureInfo.InvariantCulture
        });

        public AppContainer(
            ILogger logger,
            string packageBinaryPath,
            App app,
            Host host,
            string seqAppTypeName = null)
        {
            if (packageBinaryPath == null) throw new ArgumentNullException(nameof(packageBinaryPath));

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            App = app ?? throw new ArgumentNullException(nameof(app));
            Host = host ?? throw new ArgumentNullException(nameof(host));

            _loader = new AppLoader(packageBinaryPath);

            if (!_loader.TryLoadSeqAppType(seqAppTypeName, out var seqAppType))
                throw new ArgumentException($"The Seq app type `{seqAppTypeName}` could not be loaded.");

            _seqApp = AppActivator.CreateInstance(seqAppType, App.Title, App.Settings);
            _seqApp.Attach(this);
        }

        public App App { get; }

        public ILogger Logger { get; }

        public Host Host { get; }

        public string StoragePath => App.StoragePath;

        public void Dispose()
        {
            (_seqApp as IDisposable)?.Dispose();
            _loader.Dispose();
        }

        public async Task SendAsync(string clef)
        {
            if (clef == null) throw new ArgumentNullException(nameof(clef));

            if (_seqApp is ISubscribeToJsonAsync jled)
            {
                // Shorter, cheaper path for the "modern" interface
                try
                {
                    await jled.OnAsync(clef);
                }
                catch (Exception ex)
                {
                    ReadSerilogEvent(clef, out var eventId, out _);
                    Logger.Error(ex, "The event {EventId} could not be sent to {AppInstanceTitle}.", eventId, App.Title);
                }
            }
            else
            {
                await SendTypedEventAsync(clef);
            }
        }

        async Task SendTypedEventAsync(string clef)
        {
            var serilogEvent = ReadSerilogEvent(clef, out var eventId, out var eventType);
            try
            {
                if (_seqApp is ISubscribeTo<LogEventData> led)
                {
                    led.On(EventFormat.FromRaw(eventId, eventType, serilogEvent));
                }
                else if (_seqApp is ISubscribeToAsync<LogEventData> leda)
                {
                    await leda.OnAsync(EventFormat.FromRaw(eventId, eventType, serilogEvent));
                }
                else if (_seqApp is ISubscribeTo<LogEvent> sled)
                {
                    sled.On(new Event<LogEvent>(eventId, eventType, serilogEvent.Timestamp.UtcDateTime, serilogEvent));
                }
                else if (_seqApp is ISubscribeToAsync<LogEvent> sleda)
                {
                    await sleda.OnAsync(new Event<LogEvent>(eventId, eventType, serilogEvent.Timestamp.UtcDateTime, serilogEvent));
                }
                else
                {
                    throw new SeqAppException("The app doesn't support any recognized subscriber interfaces.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "The event {EventId} could not be sent to {AppInstanceTitle}.", eventId, App.Title);
            }
        }

        LogEvent ReadSerilogEvent(string clef, out string eventId, out uint eventType)
        {
            var jvalue = new JsonTextReader(new StringReader(clef));
            if (!(_serializer.Deserialize<JToken>(jvalue) is JObject jobject))
                throw new InvalidDataException($"The line is not a JSON object: `{clef.Trim()}`.");

            if (jobject.TryGetValue("@l", out var levelToken))
            {
                jobject.Remove("@l");
                jobject.Add("@l", new JValue(LevelMapping.ToSerilogLevel(levelToken.Value<string>()).ToString()));
            }

            var raw = LogEventReader.ReadFromJObject(jobject);

            eventId = "event-0";
            if (raw.Properties.TryGetValue("@seqid", out var id) &&
                id is ScalarValue svid &&
                svid.Value is string sid)
                eventId = sid;

            eventType = 0u;
            if (raw.Properties.TryGetValue("@i", out var et) &&
                et is ScalarValue svet &&
                svet.Value is string set &&
                uint.TryParse(set, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var uet))
                eventType = uet;

            return raw;
        }

        public void StartPublishing(TextWriter inputWriter)
        {
            if (_seqApp is IPublishJson pjson)
                pjson.Start(inputWriter);
        }

        public void StopPublishing()
        {
            if (_seqApp is IPublishJson pjson)
                pjson.Stop();
        }
    }
}
