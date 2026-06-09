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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Seq.Api.Model.Data;
using Seq.Api.Model.Events;
using Seq.Api.Model.Shared;
using SeqCli.Mcp.Data;
using SeqCli.Syntax;

namespace SeqCli.Output;

/// <summary>
/// Constructs Seq syntax literals from API events. This provides a language model client with strong cues as
/// to how the properties of an event should be incorporated into future queries/expressions.
/// </summary>
static partial class NativeFormatter
{
    static readonly object UndefinedValue = new();
    
    [GeneratedRegex("^[_a-zA-Z][_a-zA-Z0-9]*$")]
    private static partial Regex IdentifierRegex();

    static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "ci", "else", "false", "if", "in", "is", "let", "like", "not", "null", "or", "then", "true",
        "analyze", "as", "asc", "by", "desc", "explain", "for", "from", "group", "having", "into", "lateral",
        "limit", "lower", "order", "select", "where"
    };

    public static string MakeIdentifier(string prefixPath, string propertyName, bool prefixIsOptional)
    {
        if (IdentifierRegex().IsMatch(propertyName))
        {
            if (prefixIsOptional && !Keywords.Contains(propertyName))
                return propertyName;
            return $"{prefixPath}.{propertyName}";
        }

        var sw = new StringWriter();
        WriteValue(sw, propertyName);
        return $"{prefixPath}[{sw}]";
    }
    
    public static void WriteEvent(TextWriter output, EventEntity evt)
    {
        WriteObject(
            output,
            true,
            ("@Id", evt.Id),
            ("@Timestamp", DateTimeOffset.Parse(evt.Timestamp).UtcDateTime),
            ("@Level", evt.Level ?? "Information"),
            ("@Message", evt.RenderedMessage),
            ("@MessageTemplate", ReconstructTemplate(evt.MessageTemplateTokens)),
            ("@EventType", ParseEventType(evt.EventType)),
            ("@Exception", evt.Exception ?? UndefinedValue),
            ("@Elapsed", evt.Elapsed ?? UndefinedValue),
            ("@TraceId", evt.TraceId ?? UndefinedValue),
            ("@SpanId", evt.SpanId ?? UndefinedValue),
            ("@SpanKind", evt.SpanKind ?? UndefinedValue),
            ("@Start", evt.Start != null ? DateTimeOffset.Parse(evt.Start).UtcDateTime : UndefinedValue),
            ("@ParentId", evt.ParentId ?? UndefinedValue),
            ("@Properties", evt.Properties?.Count > 0 ? new Action<TextWriter>(w => WritePropertiesObject(w, evt.Properties)) : UndefinedValue),
            ("@Scope", evt.Scope?.Count > 0 ? new Action<TextWriter>(w => WritePropertiesObject(w, evt.Scope)) : UndefinedValue),
            ("@Resource", evt.Resource?.Count > 0 ? new Action<TextWriter>(w => WritePropertiesObject(w, evt.Resource)) : UndefinedValue),
            ("@Definitions", evt.Definitions?.Count > 0 ? new Action<TextWriter>(w => WritePropertiesObject(w, evt.Definitions)) : UndefinedValue)
        );
    }

    public static void WriteValue(TextWriter output, object? value)
    {
        if (value == UndefinedValue)
        {
            // This should never occur, but works in case it becomes necessary.
            output.Write("@Undefined");
            return;
        }

        switch (value)
        {
            case null:
                output.Write("null");
                return;
            case true:
                output.Write("true");
                return;
            case false:
                output.Write("false");
                return;
        }

        if (value is string s)
        {
            output.Write('\'');
            output.Write(s.Replace("'", "''"));
            output.Write('\'');
            return;
        }

        if (value is decimal
            or double or float or Half
            or byte or ushort or uint or ulong or UInt128 or
            sbyte or short or int or long or Int128)
        {
            output.Write(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
            return;
        }

        if (value is TimeSpan ts)
        {
            output.Write(DurationMoniker.FromTimeSpan(ts));
            return;
        }

        if (value is DateTime dt)
        {
            output.Write($"DateTime('{dt:O}')");
            return;
        }
        
        if (value is JArray ja)
        {
            var first = true;
            output.Write('[');
            foreach (var element in ja)
            {
                if (first)
                    first = false;
                else
                    output.Write(", ");
                WriteValue(output, element);
            }
            output.Write(']');
            return;
        }

        if (value is JObject jo)
        {
            WriteObject(output, false, jo.Properties().Select(p => (p.Name, (object?)p.Value)));
            return;
        }

        if (value is JValue jt)
        {
            WriteValue(output, jt.Value);
            return;
        }
        
        WriteValue(output, value.ToString());
    }

    static void WriteMemberName(TextWriter output, string name)
    {
        if (IdentifierRegex().IsMatch(name))
        {
            output.Write(name);
        }
        else
        {
            WriteValue(output, name);
        }
    }

    static void WritePropertiesObject(TextWriter output, List<EventPropertyPart> members)
    {
        WriteObject(output, false, members.Select(m => (m.Name, (object?)m.Value)));
    }

    static uint ParseEventType(string dollarPrefixedHex)
    {
        return uint.Parse(dollarPrefixedHex.TrimStart('$'), NumberStyles.HexNumber);
    }

    static string ReconstructTemplate(IEnumerable<MessageTemplateTokenPart> tokens)
    {
        return string.Concat(tokens.Select(t =>
            t.RawText ??
            t.Text?.Replace("{", "{{").Replace("}", "}}") ??
            $"{{{t.PropertyName}}}"));
    }

    static void WriteObject(TextWriter output, bool topLevel, params IEnumerable<(string, object?)> members)
    {
        output.Write('{');
        var first = true;
        foreach (var (name, value) in members)
        {
            if (value == UndefinedValue)
                continue;

            if (first)
                first = false;
            else
                output.Write(", ");

            if (topLevel)
            {
                output.Write(name);
            }
            else
            {
                WriteMemberName(output, name);
            }

            output.Write(": ");

            if (value is Action<TextWriter> valueWriter)
            {
                valueWriter(output);
            }
            else
            {
                WriteValue(output, value);
            }
        }
        output.Write('}');
    }

    public static void WriteQueryResult(TextWriter output, QueryResultPart result)
    {
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            QueryResultHelper.WriteErrorResult(output, result);
        }
        
        var first = true;
        QueryResultHelper.Flatten(result, row =>
        {
            if (first)
            {
                first = false;
                var firstCol = true;
                foreach (var heading in row)
                {
                    if (firstCol)
                        firstCol = false;
                    else
                        output.Write(' ');
                    output.Write(heading);
                }
            }
            else
            {
                var firstCol = true;
                foreach (var value in row)
                {
                    if (firstCol)
                        firstCol = false;
                    else
                        output.Write(' ');
                    WriteValue(output, value);
                }
            }

            output.WriteLine();
        });
    }
}
