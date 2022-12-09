// Copyright Datalust Pty Ltd and Contributors
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
using SeqCli.Templates.Ast;

namespace SeqCli.Templates.ObjectGraphs;

static class JsonTemplateObjectGraphConverter
{
    public static object? Convert(JsonTemplate template)
    {
        if (template == null) throw new ArgumentNullException(nameof(template));

        return template switch
        {
            JsonTemplateNull _ => null,
            JsonTemplateBoolean b => b.Value,
            JsonTemplateNumber n => ConvertNumber(n),
            JsonTemplateString s => s.Value,
            JsonTemplateArray a => ConvertArray(a),
            JsonTemplateObject o => ConvertObject(o),
            JsonTemplateCall c => throw new ArgumentException($"The call `{c.Name}` was not evaluated."),
            _ => throw new ArgumentOutOfRangeException(nameof(template)),
        };
    }

    static object ConvertNumber(JsonTemplateNumber template)
    {
        // This little dance helps us get the right format out of JSON.NET serialization down the line,
        // where decimals always end up with .0 even when integral (which is subsequently rejected on
        // the server-side if the target property is an integral type).
            
        if (template.Value == Math.Floor(template.Value))
        {
            if (template.Value < 0)
                return (long) template.Value;
            return (ulong) template.Value;
        }

        return template.Value;
    }

    static object ConvertArray(JsonTemplateArray template)
    {
        var r = new object?[template.Elements.Length];
        for (var i = 0; i < template.Elements.Length; ++i)
        {
            r[i] = Convert(template.Elements[i]);
        }

        return r;
    }
        
    static object ConvertObject(JsonTemplateObject template)
    {
        var r = new Dictionary<string, object?>(template.Members.Count);
        foreach (var (name, value) in template.Members)
        {
            r[name] = Convert(value);
        }

        return r;
    }
}