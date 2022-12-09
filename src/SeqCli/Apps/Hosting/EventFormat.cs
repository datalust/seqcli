// Copyright 2019 Datalust Pty Ltd
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
using System.Linq;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog.Events;
using LogEventLevel = Seq.Apps.LogEvents.LogEventLevel;

namespace SeqCli.Apps.Hosting
{
    static class EventFormat
    {
        public static Event<LogEventData> FromRaw(string eventId, uint eventType, LogEvent raw)
        {
            var properties = new Dictionary<string, object?>();
            foreach (var prop in raw.Properties)
            {
                if (prop.Key is "@seqid" or "@i")
                    continue;

                properties.Add(prop.Key, ToData(prop.Value));
            }

            var data = new LogEventData
            {
                Id = eventId,
                Level = (LogEventLevel)(int)raw.Level,
                Exception = raw.Exception?.ToString(),
                LocalTimestamp = raw.Timestamp,
                MessageTemplate = raw.MessageTemplate.Text,
                Properties = properties,
                RenderedMessage = raw.RenderMessage()
            };

            return new Event<LogEventData>(eventId, eventType, raw.Timestamp.UtcDateTime, data);
        }

        static object? ToData(LogEventPropertyValue value)
        {
            switch (value)
            {
                case ScalarValue sv:
                    return sv.Value;
                case StructureValue str:
                    var props = str.Properties.ToDictionary(kv => kv.Name, kv => ToData(kv.Value));
                    if (str.TypeTag != null)
                        props["$type"] = str.TypeTag;
                    return props;
                case SequenceValue seq:
                    return seq.Elements.Select(ToData).ToArray();
                case DictionaryValue dv:
                    var dict = new Dictionary<object, object?>();
                    foreach (var kvp in dv.Elements)
                        dict[ToData(kvp.Key)!] = ToData(kvp.Value);
                    return dict;
                default:
                    throw new NotSupportedException($"Value type {value} is not supported.");
            }
        }
    }
}
