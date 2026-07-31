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

namespace SeqCli.Traces;

/// <summary>
/// A span or log event belonging to a trace, projected from a Seq query result row.
/// </summary>
/// <param name="Id">The event id.</param>
/// <param name="Timestamp">The event timestamp; for spans, this marks span completion.</param>
/// <param name="Level">The original level string, if any.</param>
/// <param name="Message">The rendered message.</param>
/// <param name="Exception">Exception details, if any.</param>
/// <param name="SpanId">For spans, the span's own id; for logs, the id of the enclosing span, if any.</param>
/// <param name="ParentId">For spans, the id of the parent span, if any.</param>
/// <param name="Start">The span start time; <c>null</c> for log events.</param>
/// <param name="Elapsed">The span duration; <c>null</c> for log events.</param>
/// <param name="Columns">Values of the selected columns, in command-line order; a <c>null</c>
/// entry marks a value missing from the event.</param>
record TraceEvent(
    string Id,
    DateTimeOffset Timestamp,
    string? Level,
    string Message,
    string? Exception,
    string? SpanId,
    string? ParentId,
    DateTimeOffset? Start,
    TimeSpan? Elapsed,
    IReadOnlyList<object?> Columns)
{
    public bool IsSpan => Start != null;

    /// <summary>
    /// The instant used to position the event among its siblings in the trace tree.
    /// </summary>
    public DateTimeOffset SortKey => Start ?? Timestamp;
}
