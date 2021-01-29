using System;

namespace SeqCli.Templates.Ast
{
    class JsonTemplateArray : JsonTemplate
    {
        public JsonTemplate[] Elements { get; }

        public JsonTemplateArray(JsonTemplate[] elements)
        {
            Elements = elements ?? throw new ArgumentNullException(nameof(elements));
        }
    }
}