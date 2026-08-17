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
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using SeqCli.Mapping;
using SeqCli.Output;
using Serilog.Events;
using Serilog.Formatting;

namespace SeqCli.Traces;

static class TraceTreeJObjectBuilder
{
    static readonly ITextFormatter MessageFormatter = TextFormatters.Plain(theme: null, "{@m}");

    public static JObject FromRoots(string traceId, IReadOnlyList<TraceTreeNode> roots, bool complete, IReadOnlyList<string> columns)
    {
        // The (chronologically) first parentless span is the trace root; any other root-level span —
        // parent missing, duplicate parentless span, cycle participant — is an orphan.
        TraceTreeNode? root = null;
        var orphans = new JArray();
        foreach (var node in roots)
        {
            if (node.Element.IsSpan && root == null && node.Element.ParentId == null)
                root = node;
            else
                orphans.Add(ToJson(node, columns));
        }

        return new JObject
        {
            ["traceId"] = traceId,
            ["complete"] = complete,
            ["root"] = root != null ? ToJson(root, columns) : JValue.CreateNull(),
            ["orphans"] = orphans
        };
    }

    public static JObject FromSubtree(string traceId, TraceTreeNode subtree, bool complete, IReadOnlyList<string> columns)
    {
        return new JObject
        {
            ["traceId"] = traceId,
            ["complete"] = complete,
            ["root"] = ToJson(subtree, columns)
        };
    }

    static JObject ToJson(TraceTreeNode node, IReadOnlyList<string> columns)
    {
        var evt = node.Element;

        var json = new JObject
        {
            ["type"] = evt.IsSpan ? "span" : "log"
        };

        if (evt.SpanId != null)
            json["spanId"] = evt.SpanId;

        if (evt is { IsSpan: true, ParentId: not null })
            json["parentSpanId"] = evt.ParentId;

        if (!string.IsNullOrEmpty(evt.Level))
            json["level"] = LevelMapping.ToFullLevelName(evt.Level);

        if (evt.IsSpan)
        {
            json["start"] = evt.Start!.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
            if (evt.Elapsed is { } elapsed)
                json["elapsedMs"] = elapsed.TotalMilliseconds;
        }
        else
        {
            json["timestamp"] = evt.Timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
        }

        json["message"] = RenderMessage(evt);

        if (!string.IsNullOrWhiteSpace(evt.Exception))
            json["exception"] = evt.Exception;

        if (ColumnsToJson(evt, columns) is { } selected)
            json["columns"] = selected;

        if (evt.IsSpan)
        {
            var children = new JArray();
            foreach (var child in node.Children)
                children.Add(ToJson(child, columns));
            json["children"] = children;
        }

        return json;
    }

    static JObject? ColumnsToJson(TraceTreeElement evt, IReadOnlyList<string> columns)
    {
        JObject? selected = null;
        for (var i = 0; i < columns.Count && i < evt.Columns.Count; ++i)
        {
            if (evt.Columns[i] is not { } value || value is JValue { Type: JTokenType.Null })
                continue;

            selected ??= new JObject();
            selected[columns[i]] = value as JToken ?? new JValue(value);
        }

        return selected;
    }

    static string RenderMessage(TraceTreeElement evt)
    {
        var logEvent = new LogEvent(
            evt.SortKey,
            LevelMapping.ToSerilogLevel(evt.Level ?? ""),
            exception: null,
            evt.MessageTemplate,
            evt.TemplateProperties);

        var message = new StringWriter();
        MessageFormatter.Format(logEvent, message);
        return message.ToString();
    }
}
