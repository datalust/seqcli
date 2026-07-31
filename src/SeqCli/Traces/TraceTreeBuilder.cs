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
using System.Linq;

namespace SeqCli.Traces;

class TraceNode
{
    public required TraceEvent Event { get; init; }
    public TraceNode? Parent { get; set; }
    public List<TraceNode> Children { get; } = [];
}

static class TraceTreeBuilder
{
    /// <summary>
    /// Assemble events sharing a trace id into one or more trees. Spans attach to the span named
    /// by their parent id, and logs to their enclosing span. Spans with no captured parent become
    /// roots; logs with no captured enclosing span are shown under the first root span, when one
    /// exists. Siblings are ordered chronologically.
    /// </summary>
    public static IReadOnlyList<TraceNode> Build(IReadOnlyList<TraceEvent> traceEvents)
    {
        var nodes = traceEvents.Select(evt => new TraceNode { Event = evt }).ToList();

        var spansById = new Dictionary<string, TraceNode>();
        foreach (var node in nodes)
        {
            if (node.Event is { IsSpan: true, SpanId: not null })
                spansById.TryAdd(node.Event.SpanId, node);
        }

        var roots = new List<TraceNode>();
        var orphanLogs = new List<TraceNode>();
        foreach (var node in nodes)
        {
            var parentSpanId = node.Event.IsSpan ? node.Event.ParentId : node.Event.SpanId;
            if (parentSpanId != null && spansById.TryGetValue(parentSpanId, out var parent) &&
                !IsSelfOrAncestor(node, parent))
            {
                node.Parent = parent;
                parent.Children.Add(node);
            }
            else if (node.Event.IsSpan)
            {
                roots.Add(node);
            }
            else
            {
                orphanLogs.Add(node);
            }
        }

        if (orphanLogs.Count > 0)
        {
            var firstRootSpan = roots.Count > 0 ? roots.Min(Comparer<TraceNode>.Create(CompareSiblings)) : null;
            if (firstRootSpan != null)
            {
                foreach (var log in orphanLogs)
                    log.Parent = firstRootSpan;
                firstRootSpan.Children.AddRange(orphanLogs);
            }
            else
            {
                roots.AddRange(orphanLogs);
            }
        }

        SortRecursive(roots);
        return roots;
    }

    // Malformed traces can name a span as its own ancestor; attaching would loop the tree.
    static bool IsSelfOrAncestor(TraceNode node, TraceNode prospectiveParent)
    {
        for (var scan = prospectiveParent; scan != null; scan = scan.Parent)
        {
            if (ReferenceEquals(scan, node))
                return true;
        }

        return false;
    }

    static int CompareSiblings(TraceNode? x, TraceNode? y)
    {
        var byTime = x!.Event.SortKey.CompareTo(y!.Event.SortKey);
        return byTime != 0 ? byTime : string.CompareOrdinal(x.Event.Id, y.Event.Id);
    }

    static void SortRecursive(List<TraceNode> siblings)
    {
        siblings.Sort(CompareSiblings);
        foreach (var node in siblings)
            SortRecursive(node.Children);
    }
}
