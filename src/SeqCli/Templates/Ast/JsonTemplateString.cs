using System;

namespace SeqCli.Templates.Ast
{
    class JsonTemplateString : JsonTemplate
    {
        public string Value { get; }

        public JsonTemplateString(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}