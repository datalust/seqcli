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

namespace SeqCli.Traces;

/// <summary>
/// The output template used by <c>trace show</c>, along with the "hygienic" property names
/// carrying tree-specific values through to it.
/// </summary>
static class TraceShowFormat
{
    internal static readonly string TreePrefixProperty = $"_SeqcliTraceTreePrefix_{Guid.NewGuid():N}";
    internal static readonly string DurationProperty = $"_SeqcliTraceDuration_{Guid.NewGuid():N}";

    static readonly string SelectedPropertyPrefix = $"_SeqcliTraceSelected_{Guid.NewGuid():N}";

    /// <summary>
    /// The name of the event property carrying the value of the <paramref name="index"/>th
    /// selected property; these are embedded in the message template so that the property list
    /// renders in <c>{name: value}</c> style, themed.
    /// </summary>
    internal static string SelectedPropertyName(int index) => $"{SelectedPropertyPrefix}_{index}";

    public static readonly string OutputTemplate =
        $"[{{@t:HH:mm:ss}} {{@l:u3}}] {{{TreePrefixProperty}}}{{@m}}" +
        $"{{#if {DurationProperty} is not null}} ({{Milliseconds({DurationProperty}):0.###}} ms){{#end}}" +
        Environment.NewLine + "{@x}";
}
