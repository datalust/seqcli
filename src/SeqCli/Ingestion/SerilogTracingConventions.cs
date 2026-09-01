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

using System.Text.Json.Nodes;

namespace SeqCli.Ingestion;

/// <summary>
/// SerilogTracing emits span fields as regular event properties, because Serilog's data model
/// has nowhere else to put them. Events passing through seqcli lift these into the reified
/// <c>@st</c> and <c>@ps</c> fields so that they're recognized as spans by Seq and by seqcli's
/// own output formatting.
/// </summary>
static class SerilogTracingConventions
{
    internal const string ParentSpanIdProperty = "ParentSpanId";

    internal const string SpanStartTimestampProperty = "SpanStartTimestamp";

    public static void LiftSpanProperties(JsonObject eventJson)
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
