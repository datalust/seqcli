using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Seq.Api.Model.Events;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using SeqCli.Mcp.Formatting;

namespace SeqCli.Mcp;

class McpSession
{
    readonly Lock _sync = new();
    int _nextId = 1;
    readonly Dictionary<int, string> _resultIdToEventId = new();
    readonly Dictionary<string, (int, EventEntity)> _eventIdToResult = new();
    
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

    static string FormatResultId(int resultId)
    {
        return "E" + resultId.ToString("X5");
    }

    static bool TryParseResultId(string formatted, [NotNullWhen(true)] out int? resultId)
    {
        if (!formatted.StartsWith('E') || !int.TryParse(formatted.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            resultId = null;
            return false;
        }

        resultId = parsed;
        return true;
    }

    public bool TryGetSearchResult(string resultId, [NotNullWhen(true)] out EventEntity? result, [NotNullWhen(false)] out string? error)
    {
        if (!TryParseResultId(resultId, out var parsed))
        {
            result = null;
            error =
                "The result id is not correctly formatted; result ids are strings beginning with `E`, followed by a short character string.";
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

    public IEnumerable<string> EnumerateUserPropertyNames()
    {
        List<EventEntity> all;
        lock (_sync)
        {
            all = _eventIdToResult.Values.Select(pair => pair.Item2).ToList();
        }

        var seen = new HashSet<string>();
        foreach (var evt in all)
        {
            foreach (var property in evt.Properties)
            {
                foreach (var unique in EnumerateUnique(seen, "@Properties", true, property.Name, property.Value, 1))
                    yield return unique;
            }
            foreach (var property in evt.Scope)
            {
                foreach (var unique in EnumerateUnique(seen, "@Scope", false, property.Name, property.Value, 1))
                    yield return unique;
            }
            foreach (var property in evt.Resource)
            {
                foreach (var unique in EnumerateUnique(seen, "@Resource", false, property.Name, property.Value, 1))
                    yield return unique;
            }
        }
    }

    static IEnumerable<string> EnumerateUnique(HashSet<string> seen, string prefixPath, bool optionalPrefix, string propertyName, object? propertyValue, int depth)
    {
        var name = SeqSyntaxFormatter.MakeIdentifier(prefixPath, optionalPrefix, propertyName);
        if (seen.Add(name))
            yield return name;
        
        if (depth < 5 && propertyValue is JObject jo)
        {
            foreach (var child in jo.Properties())
            {
                foreach (var childName in EnumerateUnique(seen, name, false, child.Name, child.Value, depth + 1))
                    yield return childName;
            }
        }
    }
}