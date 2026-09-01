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
using System.Text;
using System.Text.Json.Nodes;
using SeqCli.Traces;
using SeqCli.Util;

namespace SeqCli.Output;

static class TraceFormatter
{
    static readonly string TreePrefixProperty = $"_SeqcliTraceTreePrefix_{Guid.NewGuid():N}";
    static readonly string ElapsedProperty = $"_SeqcliTraceElapsed_{Guid.NewGuid():N}";
    static readonly string ColumnPrefixProperty = $"_SeqcliTraceColumn_{Guid.NewGuid():N}";

    const string SpanConnector = "├─ ", LastSpanConnector = "└─ ", LogConnector = "┊  ",
        Continuation = "│  ", Gap = "   ";

    static string ColumnPropertyName(int index) => $"{ColumnPrefixProperty}_{index}";

    public static string OutputTemplate(int columnCount)
    {
        var template = new StringBuilder($"[{{@Timestamp:o}} {{@Level:u3}}] {{{TreePrefixProperty}}}");

        // `<> ''` is undefined, and hence falsy, when the property is missing; the guard thus
        // drops the column, and its trailing space, for both missing and empty values.
        for (var i = 0; i < columnCount; ++i)
        {
            var column = ColumnPropertyName(i);
            template.Append($"{{#if {column} <> ''}}{{{column}}} {{#end}}");
        }

        template.Append($"{{@Message}}{{#if {ElapsedProperty} is not null}} ({{TotalMilliseconds({ElapsedProperty}):0.###}} ms){{#end}}");
        template.Append(Environment.NewLine).Append("{@Exception}");
        return template.ToString();
    }

    public static IEnumerable<JsonObject> ToEventJson(IReadOnlyList<TraceTreeNode> roots)
    {
        foreach (var root in roots)
        {
            yield return ToEventJson(root, root.Element.IsSpan ? "" : LogConnector);
            foreach (var descendant in WalkChildren(root, ""))
                yield return descendant;
        }
    }

    static IEnumerable<JsonObject> WalkChildren(TraceTreeNode parent, string indent)
    {
        for (var i = 0; i < parent.Children.Count; ++i)
        {
            var child = parent.Children[i];
            var isLast = i == parent.Children.Count - 1;

            var connector = child.Element.IsSpan ?
                isLast ? LastSpanConnector : SpanConnector :
                LogConnector;

            yield return ToEventJson(child, indent + connector);

            foreach (var descendant in WalkChildren(child, indent + (isLast ? Gap : Continuation)))
                yield return descendant;
        }
    }

    static JsonObject ToEventJson(TraceTreeNode treeNode, string treePrefix)
    {
        var evt = treeNode.Element;

        // Spans are positioned and shown at their start time.
        var eventJson = new JsonObject
        {
            ["@t"] = evt.SortKey.ToLocalTime().ToString("o", CultureInfo.InvariantCulture),
            ["@mt"] = evt.MessageTemplate,
            [TreePrefixProperty] = treePrefix
        };

        if (!string.IsNullOrEmpty(evt.Level))
            eventJson["@l"] = evt.Level;

        if (!string.IsNullOrWhiteSpace(evt.Exception))
            eventJson["@x"] = evt.Exception;

        foreach (var (name, value) in evt.TemplateProperties)
            eventJson[name] = value?.DeepClone();

        if (evt.Elapsed is { } elapsed)
            eventJson[ElapsedProperty] = elapsed.ToString("c", CultureInfo.InvariantCulture);

        for (var i = 0; i < evt.Columns.Count; ++i)
        {
            if (evt.Columns[i] is { } value)
                eventJson[ColumnPropertyName(i)] = JsonNodes.FromApiValue(value);
        }

        return eventJson;
    }
}
