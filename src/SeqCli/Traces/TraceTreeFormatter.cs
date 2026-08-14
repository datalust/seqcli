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

using System.Collections.Generic;
using SeqCli.Mapping;
using SeqCli.Output;
using SeqCli.Util;
using Serilog.Events;

namespace SeqCli.Traces;

/// <summary>
/// Flattens a trace tree into a sequence of <see cref="LogEvent"/>s decorated for rendering with
/// <see cref="TraceShowFormat.OutputTemplate"/>. Root-level spans carry no connecting glyph, child
/// spans get box-drawing connectors, and logs are marked with a dashed rule at every depth.
/// </summary>
static class TraceTreeFormatter
{
    const string SpanConnector = "├─ ", LastSpanConnector = "└─ ", LogConnector = "┊  ",
        Continuation = "│  ", Gap = "   ";

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
            new(TraceShowFormat.TreePrefixProperty, new ScalarValue(treePrefix))
        };

        properties.AddRange(evt.TemplateProperties);

        if (evt.Elapsed is { } elapsed)
            properties.Add(new(TraceShowFormat.DurationProperty, new ScalarValue(elapsed)));

        for (var i = 0; i < evt.Columns.Count; ++i)
        {
            if (evt.Columns[i] is { } value)
                properties.Add(LogEventPropertyFactory.SafeCreate(
                    TraceShowFormat.ColumnPropertyName(i), OutputFormat.CreatePropertyValue(value)));
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
