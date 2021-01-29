using System.Collections.Generic;

namespace SeqCli.Templates.Ast
{
    class JsonTemplateObject : JsonTemplate
    {
        public IReadOnlyDictionary<string, JsonTemplate> Members { get; }

        public JsonTemplateObject(IReadOnlyDictionary<string, JsonTemplate> members)
        {
            Members = members;
        }
    }
}