// Copyright © Datalust Pty Ltd
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

using System.Text.Json.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeqCli.Syntax;

namespace SeqCli.Api;

static class ToSystemTextJson
{
    /// <summary>
    /// Convert a value deserialized by the Seq API client into its `System.Text.Json` equivalent.
    /// </summary>
    public static JsonNode? FromApiValue(object? value)
    {
        return value switch
        {
            null => null,
            JToken token => FromNewtonsoft(token),
            _ => EventJson.CreateScalar(value)
        };
    }
    
    /// Conversion helper for values retrieved through the Seq API client.
    public static JsonNode? FromNewtonsoft(JToken token)
    {
        if (token is JValue { Value: null })
            return null;

        return JsonNode.Parse(token.ToString(Formatting.None));
    }
}
