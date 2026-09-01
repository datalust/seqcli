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
using System.Globalization;
using System.Text.Json.Nodes;
using Seq.Syntax.Expressions;

namespace SeqCli.Syntax.V1;

/// <summary>
/// Functions carried over from earlier seqcli versions, where Seq.Syntax had no tracing
/// support of its own. <c>Elapsed()</c> and <c>Milliseconds()</c> remain only so that existing
/// user-supplied expressions and output templates keep working; the built-in <c>@Elapsed</c>
/// and <c>TotalMilliseconds()</c> replace them.
/// </summary>
static class TracingFunctions
{
    public static readonly NameResolver Resolver = new StaticMemberNameResolver(typeof(TracingFunctions));

    public static EvaluationResult Elapsed(JsonObject eventJson)
    {
        if (GetTimestampField(eventJson, "@t") is { } timestamp &&
            GetTimestampField(eventJson, "@st") is { } start)
        {
            return JsonValue.Create(timestamp - start)!;
        }

        return EvaluationResult.Undefined;
    }

    public static EvaluationResult Milliseconds(TimeSpan timeSpan)
    {
        // Truncates instead of rounding.
        return JsonValue.Create(timeSpan.Ticks / (decimal)TimeSpan.TicksPerMillisecond);
    }

    static DateTimeOffset? GetTimestampField(JsonObject eventJson, string field)
    {
        return eventJson.TryGetPropertyValue(field, out var node) &&
               node is JsonValue value &&
               value.TryGetValue(out string? text) &&
               DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)
            ? dto
            : null;
    }
}
