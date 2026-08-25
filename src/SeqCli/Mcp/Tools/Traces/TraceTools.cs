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
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using Seq.Api;
using Seq.Api.Client;
using Seq.Api.Model.Data;
using SeqCli.Traces;
using Serilog;

// ReSharper disable UnusedMember.Global

namespace SeqCli.Mcp.Tools.Traces;

[McpServerToolType]
class TraceTools(McpSession session, SeqConnection connection)
{
    [McpServerTool(Name = "seq_load_trace", ReadOnly = true, Title = "Load a Trace as a Tree of Spans")]
    [Description("Load a distributed trace as a JSON tree of spans. Spans are nested beneath " +
                 "their parents in chronological order. Exception details are included where present. Use" +
                 " the `seq-search-and-query` skill when calling this tool.")]
    [return: Description("A JSON document `{traceId, complete, root, orphans?}`; `complete` is false when the " +
                         "trace was truncated at the retrieval limit. Each span is " +
                         "`{spanId, parentSpanId?, level?, start, elapsedMs?, message, exception?, " +
                         "columns?, children?}`, where `columns` is keyed by the supplied column expressions.")]
    public async Task<CallToolResult> LoadTraceAsync(
        [Description("The trace id: 32 hexadecimal digits.")]
        // ReSharper disable once InconsistentNaming
        string trace_id,
        [Description("Optionally, the id of a span within the trace (16 hexadecimal digits); when specified, " +
                     "only the subtree rooted at this span is returned.")]
        // ReSharper disable once InconsistentNaming
        string? span_id = null,
        [Description("Seq expressions computed for each span and returned in the node's `columns` object; for " +
                     "example `OrderId`, `@SpanKind`, or `@Resource.service.name`.")]
        string[]? columns = null,
        CancellationToken cancellationToken = default)
    {
        var traceId = trace_id.Trim().ToLowerInvariant();
        if (!TraceQuery.IsValidTraceId(traceId))
        {
            return McpResults.SimpleText($"The trace id `{trace_id}` is not valid; trace ids are 32 hexadecimal digits.",
                isError: true);
        }

        var spanId = span_id?.Trim().ToLowerInvariant();
        if (spanId != null && !TraceQuery.IsValidSpanId(spanId))
        {
            return McpResults.SimpleText($"The span id `{span_id}` is not valid; span ids are 16 hexadecimal digits.",
                isError: true);
        }

        var columnExpressions = columns ?? [];
        if (columnExpressions.Any(string.IsNullOrWhiteSpace))
        {
            return McpResults.SimpleText("Column expressions require a value.", isError: true);
        }

        QueryResultPart result;
        try
        {
            result = await connection.Data.TryQueryAsync(
                TraceQuery.Build(traceId, includeLogs: false, includeExceptions: true, columnExpressions),
                timeout: session.DataToolCallTimeout,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex.GetBaseException() is not OperationCanceledException)
            {
                Log.Error(ex, "Exception thrown during trace retrieval");
            }

            var error = ex.GetBaseException() is SeqApiException ? ex.GetBaseException().Message : ex.ToString();
            return McpResults.SimpleText($"Could not retrieve the trace. {error}", isError: true);
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            var reasons = result.Reasons is { Length: > 0 } ? " " + string.Join(" ", result.Reasons) : "";
            return McpResults.SimpleText($"Could not retrieve the trace. {result.Error}{reasons}", isError: true);
        }

        var traceEvents = TraceQuery.ReadEvents(result, includeExceptions: true, columnExpressions);
        if (traceEvents.Count == 0)
        {
            return McpResults.SimpleText($"No events found for trace {traceId}.", isError: true);
        }

        var complete = traceEvents.Count != TraceQuery.MaxEvents;
        var roots = TraceTreeBuilder.Build(traceEvents);

        JObject document;
        if (spanId != null)
        {
            var subtreeRoot = TraceTreeBuilder.FindSpan(roots, spanId);
            if (subtreeRoot == null)
            {
                return McpResults.SimpleText($"The span {spanId} does not appear in trace {traceId}.", isError: true);
            }

            document = TraceTreeJObjectConverter.FromSubtree(traceId, subtreeRoot, complete, includeTypeMarker: false, columnExpressions);
        }
        else
        {
            document = TraceTreeJObjectConverter.FromRoots(traceId, roots, complete, includeTypeMarker: false, columnExpressions);
        }

        var status = complete
            ? $"Loaded {traceEvents.Count} span(s) from trace {traceId}."
            : $"Loaded the first {TraceQuery.MaxEvents} spans of trace {traceId}; the tree may be incomplete.";

        return McpResults.StructuredObject(status, document);
    }
}
