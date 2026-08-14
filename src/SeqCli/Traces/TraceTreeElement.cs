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
using Serilog.Events;

namespace SeqCli.Traces;

record TraceTreeElement(
    string Id,
    DateTimeOffset Timestamp,
    string? Level,
    MessageTemplate MessageTemplate,
    IReadOnlyList<LogEventProperty> TemplateProperties,
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
