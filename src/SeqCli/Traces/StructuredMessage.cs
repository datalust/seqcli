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
using System.IO;
using Newtonsoft.Json.Linq;
using SeqCli.Output;
using SeqCli.Util;
using Serilog.Events;
using Serilog.Parsing;

namespace SeqCli.Traces;

static class StructuredMessage
{
    /// <summary>
    /// Reads the token array produced by the Seq `@StructuredMessage` property
    /// into a Serilog message template, along with the property values needed to render it.
    /// </summary>
    public static (MessageTemplate Message, IReadOnlyList<LogEventProperty> Properties) Read(object? structuredMessage)
    {
        if (structuredMessage is null or JValue { Type: JTokenType.Null })
            return (new MessageTemplate([]), []);

        if (structuredMessage is not JArray tokens)
            throw new InvalidDataException($"Expected a structured message but found `{structuredMessage}`.");

        var templateTokens = new List<MessageTemplateToken>();
        var properties = new List<LogEventProperty>();
        var propertyNames = new HashSet<string>();

        foreach (var token in tokens)
        {
            if (token is JObject hole)
            {
                var name = (hole["name"] as JValue)?.Value as string ??
                    throw new InvalidDataException("A message template hole is missing its `name`.");

                // Currently ignores `formatted`.
                templateTokens.Add(new PropertyToken(name, (hole["raw"] as JValue)?.Value as string ?? $"{{{name}}}"));

                if (hole.TryGetValue("value", out var value) && propertyNames.Add(name))
                    properties.Add(LogEventPropertyFactory.SafeCreate(name, CreatePropertyValue(value)));
            }
            else if (token is JValue { Type: JTokenType.String } text)
            {
                templateTokens.Add(new TextToken((string)text.Value!));
            }
            else
            {
                throw new InvalidDataException($"Unexpected structured message token `{token}`.");
            }
        }

        TrimEnd(templateTokens);

        return (new MessageTemplate(templateTokens), properties);
    }

    static void TrimEnd(List<MessageTemplateToken> templateTokens)
    {
        while (templateTokens.Count > 0 && templateTokens[^1] is TextToken text)
        {
            var trimmed = text.Text.TrimEnd();
            if (trimmed.Length == text.Text.Length)
                break;

            templateTokens.RemoveAt(templateTokens.Count - 1);
            if (trimmed.Length > 0)
            {
                templateTokens.Add(new TextToken(trimmed));
                break;
            }
        }
    }

    static LogEventPropertyValue CreatePropertyValue(JToken value) => value is JValue scalar ?
        new ScalarValue(scalar.Value) :
        OutputFormat.CreatePropertyValue(value);
}
