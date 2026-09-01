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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Nodes;
using SeqCli.Data;
using SeqCli.Ingestion;
using SeqCli.Syntax;
using Serilog.Events;

namespace SeqCli.Sample.Ingestion;

/// <summary>
/// The sample simulation generates metric samples as Serilog events carrying their metric
/// definitions in a surrogate property, because Serilog's data model has no <c>@d</c>
/// equivalent. Events marked this way ship as metric samples rather than logs.
/// </summary>
static class MetricsMapping
{
    // Use a "hygienic" name for the definitions property to avoid collisions.
    internal static readonly string SurrogateDefinitionsProperty = $"_SeqcliMetricDefinitions_{Guid.NewGuid():N}";

    public static bool TryGetMetricSampleJson(LogEvent logEvent, [NotNullWhen(true)] out JsonObject? sample)
    {
        if (!logEvent.Properties.TryGetValue(SurrogateDefinitionsProperty, out var definitions))
        {
            sample = null;
            return false;
        }

        // Metric samples carry only a timestamp, definitions, and their dimension/value
        // properties; no message or level.
        sample = new JsonObject
        {
            ["@t"] = logEvent.Timestamp.ToString("o", CultureInfo.InvariantCulture),
            ["@d"] = SimulationEvent.ToJsonNode(definitions)
        };

        foreach (var (name, value) in logEvent.Properties)
        {
            if (name != SurrogateDefinitionsProperty)
                EventJsonDocument.SetUserProperty(sample, name, SimulationEvent.ToJsonNode(value));
        }

        return true;
    }
}
