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
            out object result,
            out string error)
        {
            (result, error) = Evaluate(template, functions);
            return error == null;
        }

        static (object, string) Evaluate(JsonTemplate template,
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            return template switch
            {
                JsonTemplateNull _ => (null, null),
                JsonTemplateBoolean b => (b.Value, null),
                JsonTemplateNumber n => (n.Value, null),
                JsonTemplateString s => (s.Value, null),
                JsonTemplateArray a => EvaluateArray(a, functions),
                JsonTemplateObject o => EvaluateObject(o, functions),
                JsonTemplateCall c => EvaluateCall(c, functions),
                _ => throw new ArgumentOutOfRangeException(nameof(template)),
            };
        }

        static (object, string) EvaluateArray(JsonTemplateArray template, 
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            var r = new object[template.Elements.Length];
            for (var i = 0; i < template.Elements.Length; ++i)
            {
                var (v, err) = Evaluate(template.Elements[i], functions);
                if (err != null)
                    return (null, err);
                r[i] = v;
            }

            return (r, null);
        }
        
        static (object, string) EvaluateObject(JsonTemplateObject template, 
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            var r = new Dictionary<string, object>(template.Members.Count);
            foreach (var (name, value) in template.Members)
            {
                var (v, err) = Evaluate(value, functions);
                if (err != null)
                    return (null, err);
                r[name] = v;
            }

            return (r, null);
        }
        
        
        static (object, string) EvaluateCall(JsonTemplateCall template, 
            IReadOnlyDictionary<string, JsonTemplateFunction> functions)
        {
            if (!functions.TryGetValue(template.Name, out var f))
                return (null, $"The function name `{template.Name}` was not recognized.");

            var args = new object[template.Arguments.Length];
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