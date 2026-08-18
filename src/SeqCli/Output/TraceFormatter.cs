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
using System.Text;
using SeqCli.Mapping;
using SeqCli.Traces;
using SeqCli.Util;
using Serilog.Events;

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
        var template = new StringBuilder($"[{{@t:o}} {{@l:u3}}] {{{TreePrefixProperty}}}");

        // `<> ''` is undefined, and hence falsy, when the property is missing; the guard thus
        // drops the column, and its trailing space, for both missing and empty values.
        for (var i = 0; i < columnCount; ++i)
        {
            var column = ColumnPropertyName(i);
            template.Append($"{{#if {column} <> ''}}{{{column}}} {{#end}}");
        }

        template.Append($"{{@m}}{{#if {ElapsedProperty} is not null}} ({{Milliseconds({ElapsedProperty}):0.###}} ms){{#end}}");
        template.Append(Environment.NewLine).Append("{@x}");
        return template.ToString();
    }

    public static IEnumerable<LogEvent> ToLogEvents(IReadOnlyList<TraceTreeNode> roots)
    {
        foreach (var root in roots)
        {
            yield return ToLogEvent(root, root.Element.IsSpan ? "" : LogConnector);
            foreach (var descendant in WalkChildren(root, ""))
                yield return descendant;
        }
    }

    static IEnumerable<LogEvent> WalkChildren(TraceTreeNode parent, string indent)
    {
        for (var i = 0; i < parent.Children.Count; ++i)
        {
            var child = parent.Children[i];
            var isLast = i == parent.Children.Count - 1;

            var connector = child.Element.IsSpan ?
                isLast ? LastSpanConnector : SpanConnector :
                LogConnector;

            yield return ToLogEvent(child, indent + connector);

            foreach (var descendant in WalkChildren(child, indent + (isLast ? Gap : Continuation)))
                yield return descendant;
        }
    }

    static LogEvent ToLogEvent(TraceTreeNode treeNode, string treePrefix)
    {
        var evt = treeNode.Element;

        var properties = new List<LogEventProperty>
        {
            new(TreePrefixProperty, new ScalarValue(treePrefix))
        };

        properties.AddRange(evt.TemplateProperties);

        if (evt.Elapsed is { } elapsed)
            properties.Add(new(ElapsedProperty, new ScalarValue(elapsed)));

        for (var i = 0; i < evt.Columns.Count; ++i)
        {
            if (evt.Columns[i] is { } value)
                properties.Add(LogEventPropertyFactory.SafeCreate(
                    ColumnPropertyName(i), OutputFormat.CreatePropertyValue(value)));
        }

        // Spans are positioned and shown at their start time.
        return new LogEvent(
            evt.SortKey.ToLocalTime(),
            LevelMapping.ToSerilogLevel(evt.Level ?? ""),
            string.IsNullOrWhiteSpace(evt.Exception) ? null : new TextException(evt.Exception),
            evt.MessageTemplate,
            properties);
    }
}
