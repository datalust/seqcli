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

/// <summary>
/// Serializes a trace tree as a single nested JSON document. The trace's root span appears under
/// <c>root</c> (<c>null</c> if the root wasn't captured), with descendant spans and log events in
/// chronologically-ordered <c>children</c> arrays. Spans that couldn't be attached beneath the root
/// are listed with their subtrees under <c>orphans</c>, and log events with no captured enclosing
/// span under <c>detachedLogs</c>.
/// </summary>
static class TraceTreeJsonFormatter
{
    static readonly ITextFormatter MessageFormatter = TextFormatters.Plain(theme: null, "{@m}");

    /// <summary>
    /// Serialize a whole trace, classifying the root-level nodes produced by
    /// <see cref="TraceTreeBuilder.Build"/>.
    /// </summary>
    /// <param name="traceId">The id of the trace.</param>
    /// <param name="roots">The root-level nodes of the trace.</param>
    /// <param name="complete">Whether all of the trace's events were retrieved.</param>
    /// <param name="columns">Column expressions selected for each event.</param>
    public static JObject ToJson(string traceId, IReadOnlyList<TraceTreeNode> roots, bool complete, IReadOnlyList<string> columns)
    {
        // The (chronologically) first parentless span is the trace root; any other root-level span —
        // parent missing, duplicate parentless span, cycle participant — is an orphan.
        TraceTreeNode? root = null;
        var orphans = new JArray();
        var detachedLogs = new JArray();
        foreach (var node in roots)
        {
            if (!node.Element.IsSpan)
                detachedLogs.Add(ToJson(node, columns));
            else if (root == null && node.Element.ParentId == null)
                root = node;
            else
                orphans.Add(ToJson(node, columns));
        }

        return new JObject
        {
            ["traceId"] = traceId,
            ["complete"] = complete,
            ["root"] = root != null ? ToJson(root, columns) : JValue.CreateNull(),
            ["orphans"] = orphans,
            ["detachedLogs"] = detachedLogs
        };
    }

    /// <summary>
    /// Serialize the subtree beneath a single selected span; <c>orphans</c> and <c>detachedLogs</c>
    /// aren't applicable and are omitted from the document.
    /// </summary>
    /// <param name="traceId">The id of the trace.</param>
    /// <param name="subtreeRoot">The span at the root of the subtree.</param>
    /// <param name="complete">Whether all of the trace's events were retrieved.</param>
    /// <param name="columns">Column expressions selected for each event.</param>
    public static JObject ToJson(string traceId, TraceTreeNode subtreeRoot, bool complete, IReadOnlyList<string> columns)
    {
        return new JObject
        {
            ["traceId"] = traceId,
            ["complete"] = complete,
            ["root"] = ToJson(subtreeRoot, columns)
        };
    }

    static JObject ToJson(TraceTreeNode node, IReadOnlyList<string> columns)
    {
        var evt = node.Element;

        // On a span, `spanId` is the span's own id and `parentSpanId` its parent's; on a log,
        // `spanId` names the enclosing span, following CLEF/OTLP conventions.
        var json = new JObject
        {
            ["type"] = evt.IsSpan ? "span" : "log"
        };

        if (evt.SpanId != null)
            json["spanId"] = evt.SpanId;

        if (evt.IsSpan && evt.ParentId != null)
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

    // Renders through the same Serilog.Expressions machinery as the plain-text output, so
    // string values are unquoted and dotted template names resolve identically.
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
