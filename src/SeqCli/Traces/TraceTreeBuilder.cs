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

static class TraceTreeBuilder
{
    /// <summary>
    /// Assemble events sharing a trace id into one or more trees. Spans attach to the span named
    /// by their parent id, and logs to their enclosing span. Spans and logs with no captured parent become
    /// roots. Siblings are ordered chronologically.
    /// </summary>
    public static IReadOnlyList<TraceTreeNode> Build(IReadOnlyList<TraceTreeElement> traceEvents)
    {
        var nodes = traceEvents.Select(evt => new TraceTreeNode { Element = evt }).ToList();

        var spansById = new Dictionary<string, TraceTreeNode>();
        foreach (var node in nodes)
        {
            if (node.Element is { IsSpan: true, SpanId: not null })
                spansById.TryAdd(node.Element.SpanId, node);
        }

        var roots = new List<TraceTreeNode>();
        foreach (var node in nodes)
        {
            var parentSpanId = node.Element.IsSpan ? node.Element.ParentId : node.Element.SpanId;
            if (parentSpanId != null && spansById.TryGetValue(parentSpanId, out var parent) &&
                !IsSelfOrAncestor(node, parent))
            {
                node.Parent = parent;
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }
        
        SortRecursive(roots);
        return roots;
    }

    /// <summary>
    /// Find the node carrying the span identified by <paramref name="spanId"/>, searching depth-first
    /// through <paramref name="roots"/>; returns <c>null</c> if no such span was captured.
    /// </summary>
    public static TraceTreeNode? FindSpan(IReadOnlyList<TraceTreeNode> roots, string spanId)
    {
        foreach (var node in roots)
        {
            if (node.Element is { IsSpan: true } element && element.SpanId == spanId)
                return node;

            if (FindSpan(node.Children, spanId) is { } descendant)
                return descendant;
        }

        return null;
    }

    // Malformed traces can name a span as its own ancestor; attaching would loop the tree.
    static bool IsSelfOrAncestor(TraceTreeNode treeNode, TraceTreeNode prospectiveParent)
    {
        for (var scan = prospectiveParent; scan != null; scan = scan.Parent)
        {
            if (ReferenceEquals(scan, treeNode))
                return true;
        }

        return false;
    }

    static int CompareSiblings(TraceTreeNode? x, TraceTreeNode? y)
    {
        var byTime = x!.Element.SortKey.CompareTo(y!.Element.SortKey);
        return byTime != 0 ? byTime : string.CompareOrdinal(x.Element.Id, y.Element.Id);
    }

    static void SortRecursive(List<TraceTreeNode> siblings)
    {
        siblings.Sort(CompareSiblings);
        foreach (var node in siblings)
            SortRecursive(node.Children);
    }
}
