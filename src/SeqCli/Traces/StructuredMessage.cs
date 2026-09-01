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
using System.Linq;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using SeqCli.Util;

namespace SeqCli.Traces;

static class StructuredMessage
{
    /// <summary>
    /// Reads the token array produced by the Seq `@StructuredMessage` property into message
    /// template text, along with the property values needed to render it. Dotted hole names
    /// are stored as nested structures, matching how message rendering resolves them.
    /// </summary>
    public static (string MessageTemplate, JsonObject Properties) Read(object? structuredMessage)
    {
        if (structuredMessage is null or JValue { Type: JTokenType.Null })
            return ("", new JsonObject());

        if (structuredMessage is not JArray tokens)
            throw new InvalidDataException($"Expected a structured message but found `{structuredMessage}`.");

        var templateTokens = new List<(bool IsText, string Text)>();
        var properties = new JsonObject();
        var propertyNames = new HashSet<string>();

        foreach (var token in tokens)
        {
            if (token is JObject hole)
            {
                var name = (hole["name"] as JValue)?.Value as string ??
                    throw new InvalidDataException("A message template hole is missing its `name`.");

                // Currently ignores `formatted`.
                templateTokens.Add((false, (hole["raw"] as JValue)?.Value as string ?? $"{{{name}}}"));

                if (hole.TryGetValue("value", out var value) && propertyNames.Add(name))
                    SetPathProperty(properties, name, JsonNodes.FromNewtonsoft(value));
            }
            else if (token is JValue { Type: JTokenType.String } text)
            {
                templateTokens.Add((true, (string)text.Value!));
            }
            else
            {
                throw new InvalidDataException($"Unexpected structured message token `{token}`.");
            }
        }

        TrimEnd(templateTokens);

        var templateText = string.Concat(templateTokens.Select(t =>
            t.IsText ? t.Text.Replace("{", "{{").Replace("}", "}}") : t.Text));

        return (templateText, properties);
    }

    // Message rendering resolves dotted hole names as paths into nested objects, so `a.b`
    // becomes member `b` of object `a`. If placing a value along the path would collide with a
    // non-object value, the hole is left unresolvable and renders as raw text.
    static void SetPathProperty(JsonObject properties, string name, JsonNode? value)
    {
        var steps = name.Split('.');
        var target = properties;
        for (var i = 0; i < steps.Length - 1; ++i)
        {
            if (target.TryGetPropertyValue(steps[i], out var next))
            {
                if (next is not JsonObject nextObject)
                    return;

                target = nextObject;
            }
            else
            {
                var nextObject = new JsonObject();
                target[steps[i]] = nextObject;
                target = nextObject;
            }
        }

        target[steps[^1]] = value;
    }

    static void TrimEnd(List<(bool IsText, string Text)> templateTokens)
    {
        while (templateTokens.Count > 0 && templateTokens[^1] is (true, var text))
        {
            var trimmed = text.TrimEnd();
            if (trimmed.Length == text.Length)
                break;

            templateTokens.RemoveAt(templateTokens.Count - 1);
            if (trimmed.Length > 0)
            {
                templateTokens.Add((true, trimmed));
                break;
            }
        }
    }
}
