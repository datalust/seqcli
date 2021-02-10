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

namespace SeqCli.Templates.Evaluator
{
    static class JsonTemplateEvaluator
    {
        public static bool TryEvaluate(
            JsonTemplate template,
            IReadOnlyDictionary<string, JsonTemplateFunction> functions,
            out JsonTemplate result,
            out string error)
        {
            (result, error) = Evaluate(template, functions);
            return error == null;
        }

        static (JsonTemplate, string) Evaluate(JsonTemplate template,
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            return template switch
            {
                JsonTemplateArray a => EvaluateArray(a, functions),
                JsonTemplateObject o => EvaluateObject(o, functions),
                JsonTemplateCall c => EvaluateCall(c, functions),
                { } other => (other, null),
                null => throw new ArgumentNullException(nameof(template))
            };
        }

        static (JsonTemplate, string) EvaluateArray(JsonTemplateArray template, 
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            var r = new JsonTemplate[template.Elements.Length];
            for (var i = 0; i < template.Elements.Length; ++i)
            {
                var (v, err) = Evaluate(template.Elements[i], functions);
                if (err != null)
                    return (null, err);
                r[i] = v;
            }

            return (new JsonTemplateArray(r), null);
        }
        
        static (JsonTemplate, string) EvaluateObject(JsonTemplateObject template, 
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            var r = new Dictionary<string, JsonTemplate>(template.Members.Count);
            foreach (var (name, value) in template.Members)
            {
                var (v, err) = Evaluate(value, functions);
                if (err != null)
                    return (null, err);
                r[name] = v;
            }

            return (new JsonTemplateObject(r), null);
        }
        
        
        static (JsonTemplate, string) EvaluateCall(JsonTemplateCall template, 
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            if (!functions.TryGetValue(template.Name, out var f))
                return (null, $"The function name `{template.Name}` was not recognized.");

            var args = new JsonTemplate[template.Arguments.Length];
            for (var i = 0; i < template.Arguments.Length; ++i)
            {
                var (v, err) = Evaluate(template.Arguments[i], functions);
                if (err != null)
                    return (null, err);
                args[i] = v;
            }

            if (!f(args, out var r, out var err2))
                return (null, err2);

            return (r, null);
        }
    }
}