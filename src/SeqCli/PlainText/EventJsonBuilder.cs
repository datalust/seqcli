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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using SeqCli.Data;
using Superpower.Model;

namespace SeqCli.PlainText;

/// <summary>
/// Assembles the values captured by a plain-text extraction pattern into an event JSON
/// document in Seq's emission schema.
/// </summary>
static class EventJsonBuilder
{
    public static JsonObject FromProperties(IDictionary<string, object?> properties, string? remainder)
    {
        var eventJson = new JsonObject
        {
            ["@t"] = GetTimestamp(properties).ToString("o", CultureInfo.InvariantCulture)
        };

        if (TryGetText(properties, ReifiedProperties.Level, out var level))
            eventJson["@l"] = level;

        if (TryGetText(properties, ReifiedProperties.Message, out var message))
            eventJson["@m"] = message;

        if (TryGetText(properties, ReifiedProperties.Exception, out var exception))
            eventJson["@x"] = exception;

        if (TryGetText(properties, ReifiedProperties.TraceId, out var traceId))
            eventJson["@tr"] = traceId;

        if (TryGetText(properties, ReifiedProperties.SpanId, out var spanId))
            eventJson["@sp"] = spanId;

        if (TryGetText(properties, ReifiedProperties.StartTimestamp, out var start))
            eventJson["@st"] = start;

        foreach (var (name, value) in properties)
        {
            if (!ReifiedProperties.IsReifiedProperty(name))
                EventJsonDocument.SetUserProperty(eventJson, name, CreateValue(value));
        }

        if (remainder != null)
            EventJsonDocument.SetUserProperty(eventJson, "@unmatched", remainder);

        return eventJson;
    }

    static JsonNode? CreateValue(object? value)
    {
        return value is TextSpan span
            ? JsonValue.Create(span.ToStringValue())
            : EventJsonDocument.CreateScalar(value);
    }

    static bool TryGetText(IDictionary<string, object?> properties, string name, out string text)
    {
        if (properties.TryGetValue(name, out var value) && value is TextSpan span)
        {
            text = span.ToStringValue();
            return true;
        }

        text = "";
        return false;
    }

    static DateTimeOffset GetTimestamp(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.Timestamp, out var t))
        {
            if (t is TextSpan span && DateTimeOffset.TryParse(span.ToStringValue(),
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var ts))
                return ts;

            if (t is DateTimeOffset dto)
                return dto;
        }

        return DateTimeOffset.Now;
    }
}
