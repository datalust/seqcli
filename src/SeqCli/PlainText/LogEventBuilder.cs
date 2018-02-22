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
using System.Globalization;
using System.Linq;
using Serilog.Events;
using Serilog.Parsing;
using Superpower.Model;

namespace SeqCli.PlainText
{
    static class LogEventBuilder
    {
        public static LogEvent FromProperties(IDictionary<string, object> properties, string remainder)
        {
            var timestamp = GetTimestamp(properties);
            var level = GetLevel(properties);          
            var exception = TryGetException(properties);          
            var messageTemplate = GetMessageTemplate(properties);          
            var props = GetLogEventProperties(properties, remainder);

            return new LogEvent(
                timestamp,
                level,
                exception,
                messageTemplate,
                props);
        }
        
        static readonly MessageTemplate NoMessage = new MessageTemplateParser().Parse("");

        static MessageTemplate GetMessageTemplate(IDictionary<string, object> properties)
        {
            if (properties.TryGetValue(ReifiedProperties.Message, out var m) &&
                m is TextSpan ts)
            {
                var text = ts.ToStringValue();
                return new MessageTemplate(new MessageTemplateToken[] {new TextToken(text) });
            }

            return NoMessage;
        }

        static LogEventLevel GetLevel(IDictionary<string, object> properties)
        {
            if (properties.TryGetValue(ReifiedProperties.Level, out var l) &&
                l is TextSpan ts &&
                LevelsByName.TryGetValue(ts.ToStringValue(), out var level))
                return level;
            return LogEventLevel.Information;
        }

        static Exception TryGetException(IDictionary<string, object> properties)
        {
            if (properties.TryGetValue(ReifiedProperties.Exception, out var x) &&
                x is TextSpan ts)
                return new TextOnlyException(ts.ToStringValue());
            return null;
        }

        static IEnumerable<LogEventProperty> GetLogEventProperties(IDictionary<string, object> properties, string remainder)
        {
            var payload = properties
                .Where(p => !ReifiedProperties.IsReifiedProperty(p.Key))
                .Select(p => new LogEventProperty(p.Key, new ScalarValue(p.Value)));

            if (remainder != null)
                payload = payload.Concat(new[]
                {
                    new LogEventProperty("@unmatched", new ScalarValue(remainder))
                });
            return payload;
        }

        static DateTimeOffset GetTimestamp(IDictionary<string, object> properties)
        {
            var timestamp = properties.TryGetValue(ReifiedProperties.Timestamp, out var t) &&
                            t is TextSpan span &&
                            DateTimeOffset.TryParseExact(span.ToStringValue(), "o", CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out var ts)
                ? ts
                : DateTimeOffset.Now;
            return timestamp;
        }
                
        static readonly Dictionary<string, LogEventLevel> LevelsByName = new Dictionary<string, LogEventLevel>(StringComparer.OrdinalIgnoreCase)
        {
            ["t"] = LogEventLevel.Verbose,
            ["tr"] = LogEventLevel.Verbose,
            ["trc"] = LogEventLevel.Verbose,
            ["trce"] = LogEventLevel.Verbose,
            ["trace"] = LogEventLevel.Verbose,
            ["v"] = LogEventLevel.Verbose,
            ["ver"] = LogEventLevel.Verbose,
            ["vrb"] = LogEventLevel.Verbose,
            ["verb"] = LogEventLevel.Verbose,
            ["verbose"] = LogEventLevel.Verbose,
            ["d"] = LogEventLevel.Debug,
            ["de"] = LogEventLevel.Debug,
            ["dbg"] = LogEventLevel.Debug,
            ["deb"] = LogEventLevel.Debug,
            ["dbug"] = LogEventLevel.Debug,
            ["debu"] = LogEventLevel.Debug,
            ["debub"] = LogEventLevel.Debug,
            ["i"] = LogEventLevel.Information,
            ["in"] = LogEventLevel.Information,
            ["inf"] = LogEventLevel.Information,
            ["info"] = LogEventLevel.Information,
            ["information"] = LogEventLevel.Information,
            ["w"] = LogEventLevel.Warning,
            ["wa"] = LogEventLevel.Warning,
            ["war"] = LogEventLevel.Warning,
            ["wrn"] = LogEventLevel.Warning,
            ["warn"] = LogEventLevel.Warning,
            ["warning"] = LogEventLevel.Warning,
            ["e"] = LogEventLevel.Error,
            ["er"] = LogEventLevel.Error,
            ["err"] = LogEventLevel.Error,
            ["erro"] = LogEventLevel.Error,
            ["eror"] = LogEventLevel.Error,
            ["error"] = LogEventLevel.Error,
            ["f"] = LogEventLevel.Fatal,
            ["fa"] = LogEventLevel.Fatal,
            ["ftl"] = LogEventLevel.Fatal,
            ["fat"] = LogEventLevel.Fatal,
            ["fatl"] = LogEventLevel.Fatal,
            ["fatal"] = LogEventLevel.Fatal,
            ["c"] = LogEventLevel.Fatal,
            ["cr"] = LogEventLevel.Fatal,
            ["crt"] = LogEventLevel.Fatal,
            ["cri"] = LogEventLevel.Fatal,
            ["crit"] = LogEventLevel.Fatal,
            ["critical"] = LogEventLevel.Fatal
        };

    }
}