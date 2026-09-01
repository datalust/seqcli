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
using System.Globalization;
using System.Text.Json.Nodes;

namespace SeqCli.Syntax;

/// <summary>
/// Helpers for constructing event JSON documents in Seq's emission (CLEF) schema, where
/// reified fields carry <c>@</c>-prefixed names and user-defined property names beginning
/// with <c>@</c> are escaped with a second <c>@</c>.
/// </summary>
static class EventJson
{
    const string InvalidPropertyNameSubstitute = "(unnamed)";

    public static string EscapeUserPropertyName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return InvalidPropertyNameSubstitute;

        return name.StartsWith('@') ? $"@{name}" : name;
    }

    public static void SetUserProperty(JsonObject eventJson, string name, JsonNode? value)
    {
        eventJson[EscapeUserPropertyName(name)] = value;
    }

    public static JsonNode? CreateScalar(object? value)
    {
        return value switch
        {
            null => null,
            string s => JsonValue.Create(s),
            bool b => JsonValue.Create(b),
            byte n => JsonValue.Create(n),
            sbyte n => JsonValue.Create(n),
            short n => JsonValue.Create(n),
            ushort n => JsonValue.Create(n),
            int n => JsonValue.Create(n),
            uint n => JsonValue.Create(n),
            long n => JsonValue.Create(n),
            ulong n => JsonValue.Create(n),
            float n => JsonValue.Create(n),
            double n => JsonValue.Create(n),
            decimal n => JsonValue.Create(n),
            DateTime dt => JsonValue.Create(dt.ToString("o", CultureInfo.InvariantCulture)),
            DateTimeOffset dto => JsonValue.Create(dto.ToString("o", CultureInfo.InvariantCulture)),
            _ => JsonValue.Create(value.ToString())
        };
    }
}
