// Copyright 2018 Datalust Pty Ltd
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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SeqCli.Levels;
using SeqCli.Util;
using Serilog.Events;
using Serilog.Parsing;
using Superpower.Model;

namespace SeqCli.PlainText.LogEvents;

static class LogEventBuilder
{
    public static LogEvent FromProperties(IDictionary<string, object?> properties, string? remainder)
    {
        var timestamp = GetTimestamp(properties);
        var level = GetLevel(properties);          
        var exception = TryGetException(properties);          
        var messageTemplate = GetMessageTemplate(properties);
        var traceId = GetTraceId(properties);
        var spanId = GetSpanId(properties);
        var props = GetLogEventProperties(properties, remainder, level);

        var fallbackMappedLevel = level != null ? LevelMapping.ToSerilogLevel(level) : LogEventLevel.Information;

        return new LogEvent(
            timestamp,
            fallbackMappedLevel,
            exception,
            messageTemplate,
            props,
            traceId ?? default,
            spanId ?? default
        );
    }
        
    static readonly MessageTemplate NoMessage = new MessageTemplateParser().Parse("");

    static MessageTemplate GetMessageTemplate(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.Message, out var m) &&
            m is TextSpan ts)
        {
            var text = ts.ToStringValue();
            return new MessageTemplate(new MessageTemplateToken[] {new TextToken(text) });
        }

        return NoMessage;
    }

    static string? GetLevel(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.Level, out var l) &&
            l is TextSpan ts)
            return ts.ToStringValue();

        return null;
    }

    static ActivityTraceId? GetTraceId(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.TraceId, out var tr) && 
            tr is TextSpan ts)
            return ActivityTraceId.CreateFromString(ts.ToStringValue());

        return null;
    }
    
    static ActivitySpanId? GetSpanId(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.SpanId, out var sp) && 
            sp is TextSpan ts)
            return ActivitySpanId.CreateFromString(ts.ToStringValue());

        return null;
    }

    static Exception? TryGetException(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.Exception, out var x) &&
            x is TextSpan ts)
            return new TextOnlyException(ts.ToStringValue());
        return null;
    }

    static IEnumerable<LogEventProperty> GetLogEventProperties(IDictionary<string, object?> properties, string? remainder, string? level)
    {
        var payload = properties
            .Where(p => !ReifiedProperties.IsReifiedProperty(p.Key))
            .Select(p => LogEventPropertyFactory.SafeCreate(p.Key, new ScalarValue(p.Value)));

        if (remainder != null)
            payload = payload.Concat(new[]
            {
                LogEventPropertyFactory.SafeCreate("@unmatched", new ScalarValue(remainder))
            });
        return payload;
    }

    static DateTimeOffset GetTimestamp(IDictionary<string, object?> properties)
    {
        if (properties.TryGetValue(ReifiedProperties.Timestamp, out var t))
        {
            if (t is TextSpan span && DateTimeOffset.TryParse(span.ToStringValue(),
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var ts))
                return ts;

            if (t is DateTimeOffset dto)
                return dto;
        }
            
        return DateTimeOffset.Now;
    }
}