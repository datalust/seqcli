using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using SeqCli.Forwarder.Util;
using Serilog.Parsing;

namespace SeqCli.Forwarder.Schema
{
    static class EventSchema
    {
        static readonly MessageTemplateParser MessageTemplateParser = new MessageTemplateParser();
        
        static readonly HashSet<string> ClefReifiedProperties = new HashSet<string>
        {
            "@t", "@m", "@mt", "@l", "@x", "@i", "@r"
        };
        
        public static bool FromClefFormat(in int lineNumber, JObject compactFormat, [MaybeNullWhen(false)] out JObject rawFormat, [MaybeNullWhen(true)] out string error)
        {
            var result = new JObject();

            var rawTimestamp = compactFormat["@t"];
            if (rawTimestamp == null)
            {
                error = $"The event on line {lineNumber} does not carry an `@t` timestamp property.";
                rawFormat = default;
                return false;
            }

            if (rawTimestamp.Type != JTokenType.String)
            {
                error = $"The event on line {lineNumber} has an invalid `@t` timestamp property; the value must be a JSON string.";
                rawFormat = default;
                return false;
            }

            if (!DateTimeOffset.TryParse(rawTimestamp.Value<string>(), out _))
            {
                error = $"The timestamp value `{rawTimestamp}` on line {lineNumber} could not be parsed.";
                rawFormat = default;
                return false;
            }

            result.Add("Timestamp", rawTimestamp);

            var properties = new JObject();
            foreach (var property in compactFormat.Properties())
            {
                if (property.Name.StartsWith("@@"))
                    properties.Add(property.Name.Substring(1), property.Value);
                else if (!ClefReifiedProperties.Contains(property.Name))
                    properties.Add(property.Name, property.Value);
            }

            var x = compactFormat["@x"];
            if (x != null)
            {
                if (x.Type != JTokenType.String)
                {
                    error = $"The event on line {lineNumber} has a non-string `@x` exception property.";
                    rawFormat = default;
                    return false;
                }

                result.Add("Exception", x);
            }

            var l = compactFormat["@l"];
            if (l != null)
            {
                if (l.Type != JTokenType.String)
                {
                    error = $"The event on line {lineNumber} has a non-string `@l` level property.";
                    rawFormat = default;
                    return false;
                }

                result.Add("Level", l);
            }

            string? message = null;
            var m = compactFormat["@m"];
            if (m != null)
            {
                if (m.Type != JTokenType.String)
                {
                    error = $"The event on line {lineNumber} has a non-string `@m` message property.";
                    rawFormat = default;
                    return false;
                }

                message = m.Value<string>();
            }

            string? messageTemplate = null;
            var mt = compactFormat["@mt"];
            if (mt != null)
            {
                if (mt.Type != JTokenType.String)
                {
                    error = $"The event on line {lineNumber} has a non-string `@mt` message template property.";
                    rawFormat = default;
                    return false;
                }

                messageTemplate = mt.Value<string>();
            }

            if (message != null)
            {
                result.Add("RenderedMessage", message);
            }
            else if (messageTemplate != null && compactFormat["@r"] is JArray renderingsArray)
            {
                var template = MessageTemplateParser.Parse(messageTemplate);
                var withFormat = template.Tokens.OfType<PropertyToken>().Where(pt => pt.Format != null);

                // ReSharper disable once PossibleMultipleEnumeration
                if (withFormat.Count() == renderingsArray.Count)
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    var renderingsByProperty = withFormat
                        .Zip(renderingsArray, (p, j) => new { p.PropertyName, Format = p.Format!, Rendering = j.Value<string>() })
                        .GroupBy(p => p.PropertyName)
                        .ToDictionary(g => g.Key, g => g.ToDictionaryDistinct(p => p.Format, p => p.Rendering));

                    var renderings = new JObject();
                    result.Add("Renderings", renderings);

                    foreach (var (property, propertyRenderings) in renderingsByProperty)
                    {
                        var byFormat = new JArray();
                        renderings.Add(property, byFormat);

                        foreach (var (format, rendering) in propertyRenderings)
                        {
                            var element = new JObject {{"Format", format}, {"Rendering", rendering}};
                            byFormat.Add(element);
                        }
                    }
                }
            }

            messageTemplate ??= message ?? "No template provided";
            result.Add("MessageTemplate", messageTemplate);

            var eventTypeToken = compactFormat["@i"];
            if (eventTypeToken != null)
            {
                if (eventTypeToken.Type == JTokenType.Integer)
                {
                    result.Add("EventType", uint.Parse(eventTypeToken.Value<string>()!));
                }
                else if (eventTypeToken.Type == JTokenType.String)
                {
                    if (uint.TryParse(eventTypeToken.Value<string>(), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out var eventType))
                    {
                        result.Add("EventType", eventType);
                    }
                    else
                    {
                        // Seq would calculate a hash value from the string, here. Forwarder will ignore that
                        // case and preserve the value in an `@i` property for now.
                        result.Add("@i", eventTypeToken);
                    }
                }
                else
                {
                    error = $"The `@i` event type value on line {lineNumber} is not in a string or numeric format.";
                    rawFormat = default;
                    return false;
                }
            }

            if (properties.Count != 0)
                result.Add("Properties", properties);

            rawFormat = result;
            error = null;
            return true;
        }
    }
}
