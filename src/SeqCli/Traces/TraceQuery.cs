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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Data;

namespace SeqCli.Traces;

/// <summary>
/// Constructs the query used to retrieve the events in a trace, and reads events back out of the
/// query result. The select list in <see cref="Build"/> and the row layout expected by
/// <see cref="ReadEvents"/> must be kept in sync.
/// </summary>
static partial class TraceQuery
{
    /// <summary>
    /// The maximum number of events retrieved for a single trace.
    /// </summary>
    public const int MaxEvents = 10000;

    // Excludes the optional `@Exception` column.
    const int FixedColumnCount = 8;

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex TraceIdFormat { get; }

    /// <summary>
    /// Check whether <paramref name="traceId"/> is a well-formed (lowercase, 32-hex-digit) trace id.
    /// </summary>
    public static bool IsValidTraceId(string traceId) => TraceIdFormat.IsMatch(traceId);

    /// <summary>
    /// Build the query retrieving events in the trace identified by <paramref name="traceId"/>.
    /// </summary>
    /// <param name="traceId">The trace id; must be validated with <see cref="IsValidTraceId"/> first.</param>
    /// <param name="includeLogs">Whether log events are included, or only spans.</param>
    /// <param name="includeExceptions">Whether exception details are retrieved.</param>
    /// <param name="columns">Column expressions to select for each event.</param>
    public static string Build(string traceId, bool includeLogs, bool includeExceptions, IReadOnlyList<string> columns)
    {
        if (!IsValidTraceId(traceId)) throw new ArgumentException("The trace id has not been validated.");

        var query = new StringBuilder("select @Id, @Timestamp, @Level, @Message");

        if (includeExceptions)
            query.Append(", @Exception");

        query.Append(", @SpanId, @ParentId, @Start, @Elapsed");

        for (var i = 0; i < columns.Count; ++i)
            query.Append($", {columns[i]} as c{i}");

        query.Append($" from stream where @TraceId = '{traceId}'");

        if (!includeLogs)
            query.Append(" and @Start is not null");

        query.Append($" limit {MaxEvents}");

        return query.ToString();
    }

    /// <summary>
    /// Read the events from a result produced by the query constructed in <see cref="Build"/>.
    /// </summary>
    public static IReadOnlyList<TraceEvent> ReadEvents(QueryResultPart result, bool includeExceptions, IReadOnlyList<string> columns)
    {
        if (result.Rows == null)
            return [];

        // When exceptions are retrieved, `@Exception` shifts the columns following it.
        var e = includeExceptions ? 1 : 0;

        var events = new List<TraceEvent>(result.Rows.Length);
        foreach (var row in result.Rows)
        {
            var selected = new object?[columns.Count];
            for (var i = 0; i < columns.Count && FixedColumnCount + e + i < row.Length; ++i)
            {
                var value = row[FixedColumnCount + e + i];
                if (value is null or JValue { Type: JTokenType.Null })
                    continue;

                selected[i] = value;
            }

            events.Add(new TraceEvent(
                Id: ReadString(row[0]) ?? throw new InvalidDataException("The event id is missing."),
                Timestamp: ReadTimestamp(row[1]) ?? throw new InvalidDataException("The event timestamp is missing."),
                Level: ReadString(row[2]),
                Message: (ReadString(row[3]) ?? "").TrimEnd(),
                Exception: includeExceptions ? ReadString(row[4]) : null,
                SpanId: ReadString(row[4 + e]),
                ParentId: ReadString(row[5 + e]),
                Start: ReadTimestamp(row[6 + e]),
                Elapsed: ReadDuration(row[7 + e]),
                Columns: selected));
        }

        return events;
    }

    static string? ReadString(object? value) => value switch
    {
        null => null,
        string s => s,
        JValue jv => jv.Value?.ToString(),
        _ => value.ToString()
    };

    // The query API returns timestamps and durations as ticks (100 ns units); timestamps are since 0001-01-01 UTC.
    static DateTimeOffset? ReadTimestamp(object? value) => value switch
    {
        null => null,
        long ticks => new DateTimeOffset(ticks, TimeSpan.Zero),
        _ => throw new InvalidDataException($"Expected a timestamp in ticks but found `{value}`.")
    };

    static TimeSpan? ReadDuration(object? value) => value switch
    {
        null => null,
        long ticks => TimeSpan.FromTicks(ticks),
        _ => throw new InvalidDataException($"Expected a duration in ticks but found `{value}`.")
    };
}
