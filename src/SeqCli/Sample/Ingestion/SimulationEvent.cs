// Copyright © Datalust and contributors.
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

using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using SeqCli.Data;
using SeqCli.Syntax;
using Serilog.Events;

namespace SeqCli.Sample.Ingestion;

/// <summary>Used only in the Roastery simulation; no other event data should ever be processed using this type.</summary>
static class SimulationEvent
{
    const string ParentSpanIdProperty = "ParentSpanId", 
        SpanStartTimestampProperty = "SpanStartTimestamp";

    public static JsonObject ToJsonObject(LogEvent logEvent)
    {
        var eventJson = new JsonObject
        {
            ["@t"] = logEvent.Timestamp.ToString("o", CultureInfo.InvariantCulture),
            ["@mt"] = logEvent.MessageTemplate.Text
        };

        if (logEvent.Level != LogEventLevel.Information)
            eventJson["@l"] = logEvent.Level.ToString();

        if (logEvent.Exception != null)
            eventJson["@x"] = logEvent.Exception.ToString();

        if (logEvent.TraceId is { } traceId)
            eventJson["@tr"] = traceId.ToHexString();

        if (logEvent.SpanId is { } spanId)
            eventJson["@sp"] = spanId.ToHexString();

        foreach (var (name, value) in logEvent.Properties)
            EventJsonDocument.SetUserProperty(eventJson, name, ToJsonNode(value));

        LiftSpanProperties(eventJson);

        return eventJson;
    }

    public static JsonNode? ToJsonNode(LogEventPropertyValue value)
    {
        switch (value)
        {
            case ScalarValue scalar:
                return EventJsonDocument.CreateScalar(scalar.Value);

            case SequenceValue sequence:
                return new JsonArray(sequence.Elements.Select(ToJsonNode).ToArray());

            case StructureValue structure:
            {
                var result = new JsonObject();
                foreach (var property in structure.Properties)
                    result[property.Name] = ToJsonNode(property.Value);
                if (structure.TypeTag != null)
                    result["$type"] = structure.TypeTag;
                return result;
            }

            case DictionaryValue dictionary:
            {
                var result = new JsonObject();
                foreach (var (key, element) in dictionary.Elements)
                    result[key.Value?.ToString() ?? "null"] = ToJsonNode(element);
                return result;
            }

            default:
                return EventJsonDocument.CreateScalar(value.ToString());
        }
    }
    
    static void LiftSpanProperties(JsonObject eventJson)
    {
        LiftProperty(eventJson, SpanStartTimestampProperty, "@st");
        LiftProperty(eventJson, ParentSpanIdProperty, "@ps");
    }

    static void LiftProperty(JsonObject eventJson, string propertyName, string reifiedName)
    {
        if (eventJson.TryGetPropertyValue(propertyName, out var value))
        {
            eventJson.Remove(propertyName);
            if (!eventJson.ContainsKey(reifiedName))
                eventJson[reifiedName] = value;
        }
    }
}
