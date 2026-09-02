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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Seq.Api.Model.Events;
using Seq.Api.Model.Shared;
using SeqCli.Api;
using SeqCli.Data;
using SeqCli.Output;

namespace SeqCli.Mapping;

/// <summary>
/// Converts event entities into compact JSON format for further processing. This class is only necessary because
/// <c>Seq.Api</c> doesn't yet provide a simple compact-JSON based result format for searches. Once we've filled
/// that gap, this class, and <see cref="OutputFormat.WriteEventEntity"/> can be removed.
/// </summary>
static class EventEntityJson
{
    public static JsonObject ToEventJson(EventEntity evt)
    {
        var eventJson = new JsonObject
        {
            // Earlier versions relied on Serilog output formatting to show timestamps in local time; we'll need
            // to consider adding some compensating mechanism to `Seq.Syntax`.
            ["@t"] = DateTimeOffset.ParseExact(evt.Timestamp, "o", CultureInfo.InvariantCulture)
                .ToLocalTime().ToString("o", CultureInfo.InvariantCulture)
        };

        if (evt.MessageTemplateTokens != null)
            eventJson["@mt"] = ToMessageTemplateText(evt.MessageTemplateTokens);

        if (!string.IsNullOrWhiteSpace(evt.Level) && evt.Level != "Information")
            eventJson["@l"] = evt.Level;

        if (!string.IsNullOrWhiteSpace(evt.Exception))
            eventJson["@x"] = evt.Exception;

        if (!string.IsNullOrWhiteSpace(evt.TraceId))
            eventJson["@tr"] = evt.TraceId;

        if (!string.IsNullOrWhiteSpace(evt.SpanId))
            eventJson["@sp"] = evt.SpanId;

        if (!string.IsNullOrWhiteSpace(evt.ParentId))
            eventJson["@ps"] = evt.ParentId;

        if (!string.IsNullOrWhiteSpace(evt.Start))
            eventJson["@st"] = evt.Start;

        if (!string.IsNullOrWhiteSpace(evt.SpanKind))
            eventJson["@sk"] = evt.SpanKind;

        if (evt.Resource?.Count > 0)
            eventJson["@ra"] = ToPropertiesObject(evt.Resource);

        if (evt.Scope?.Count > 0)
            eventJson["@sa"] = ToPropertiesObject(evt.Scope);

        if (evt.Properties != null)
        {
            foreach (var property in evt.Properties)
                eventJson[EventJsonFormat.EscapeUserPropertyName(property.Name)] = ToSystemTextJson.FromApiValue(property.Value);
        }

        return eventJson;
    }

    static string ToMessageTemplateText(List<MessageTemplateTokenPart> tokens)
    {
        var text = new StringBuilder();
        foreach (var token in tokens)
        {
            if (token.Text != null)
                text.Append(token.Text.Replace("{", "{{").Replace("}", "}}"));
            else
                text.Append(token.RawText ?? $"{{{token.PropertyName}}}");
        }

        return text.ToString();
    }

    static JsonObject ToPropertiesObject(List<EventPropertyPart> properties)
    {
        var result = new JsonObject();
        foreach (var property in properties)
            result[property.Name] = ToSystemTextJson.FromApiValue(property.Value);
        return result;
    }
}
