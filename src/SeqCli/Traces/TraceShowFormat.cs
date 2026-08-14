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
using System.Text;

namespace SeqCli.Traces;

/// <summary>
/// The output template used by <c>trace show</c>, along with the "hygienic" property names
/// carrying tree-specific values through to it.
/// </summary>
static class TraceShowFormat
{
    internal static readonly string TreePrefixProperty = $"_SeqcliTraceTreePrefix_{Guid.NewGuid():N}";
    internal static readonly string DurationProperty = $"_SeqcliTraceDuration_{Guid.NewGuid():N}";

    static readonly string ColumnPrefix = $"_SeqcliTraceColumn_{Guid.NewGuid():N}";

    /// <summary>
    /// The name of the event property carrying the value of the <paramref name="index"/>th
    /// column; the output template includes a hole for each of these.
    /// </summary>
    internal static string ColumnPropertyName(int index) => $"{ColumnPrefix}_{index}";

    /// <summary>
    /// Construct the output template, including a space-separated column ahead of the event
    /// message for each of <paramref name="columnCount"/> selected columns.
    /// </summary>
    public static string OutputTemplate(int columnCount)
    {
        var template = new StringBuilder($"[{{@t:o}} {{@l:u3}}] {{{TreePrefixProperty}}}");

        // `<> ''` is undefined, and hence falsy, when the property is missing; the guard thus
        // drops the column, and its trailing space, for both missing and empty values.
        for (var i = 0; i < columnCount; ++i)
        {
            var column = ColumnPropertyName(i);
            template.Append($"{{#if {column} <> ''}}{{{column}}} {{#end}}");
        }

        template.Append($"{{@m}}{{#if {DurationProperty} is not null}} ({{Milliseconds({DurationProperty}):0.###}} ms){{#end}}");
        template.Append(Environment.NewLine).Append("{@x}");
        return template.ToString();
    }
}
