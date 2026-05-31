using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Seq.Api.Model.Events;
using System;
using System.Linq;
using SeqCli.Mcp.Schema;

namespace SeqCli.Mcp;

class McpSession
{
    readonly Lock _sync = new();
    int _nextId = 1;
    readonly Dictionary<int, string> _resultIdToEventId = new();
    readonly Dictionary<string, (int, EventEntity)> _eventIdToResult = new();

    public void Clear()
    {
        lock (_sync)
        {
            _resultIdToEventId.Clear();
            _eventIdToResult.Clear();
            // Note that `_nextId` is intentionally preserved.
        }
    }

    public string ImportSearchResult(EventEntity evt)
    {
        lock (_sync)
        {
            if (_eventIdToResult.TryGetValue(evt.Id, out var existing))
                return FormatResultId(existing.Item1);
            var resultId = _nextId;
            _nextId += 1;
            _resultIdToEventId.Add(resultId, evt.Id);
            _eventIdToResult.Add(evt.Id, (resultId, evt));
            return FormatResultId(resultId);
        }
    }

    internal static string FormatResultId(int resultId)
    {
        return "R" + resultId.ToString("X5");
    }

    internal static bool TryParseResultId(string formatted, [NotNullWhen(true)] out int? resultId)
    {
        if (!formatted.StartsWith('R') || !int.TryParse(formatted[1..], NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var parsed))
        {
            resultId = null;
            return false;
        }

        resultId = parsed;
        return true;
    }

    public bool TryGetSearchResult(string resultId, [NotNullWhen(true)] out EventEntity? result,
        [NotNullWhen(false)] out string? error)
    {
        if (!TryParseResultId(resultId, out var parsed))
        {
            result = null;
            error =
                "The result id is not correctly formatted; result ids are strings beginning with `R`, followed by a short character string.";
            return false;
        }

        lock (_sync)
        {
            if (!_resultIdToEventId.TryGetValue(parsed.Value, out var eventId))
            {
                result = null;
                error =
                    "A matching result wasn't found among recent searches. Try retrieving a fresh result id by searching again (using a very narrow time range if possible).";
                return false;
            }

            if (!_eventIdToResult.TryGetValue(eventId, out var pair))
                throw new InvalidOperationException("Missing result mapping.");

            result = pair.Item2;
            error = null;
            return true;
        }
    }

    public IEnumerable<string> EnumerateUserPropertyNames(CancellationToken cancellationToken)
    {
        List<EventEntity> all;
        lock (_sync)
        {
            all = _eventIdToResult.Values.Select(pair => pair.Item2).ToList();
        }

        var seen = new HashSet<string>();
        foreach (var evt in all)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var accessor in EventEntitySchema.EnumeratePropertyAccessorPaths(evt))
            {
                if (seen.Add(accessor))
                {
                    yield return accessor;
                }
            }
        }
    }
}