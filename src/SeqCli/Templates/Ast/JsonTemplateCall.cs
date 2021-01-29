using System;

namespace SeqCli.Templates.Ast
{
    class JsonTemplateCall : JsonTemplate
    {
        public string Name { get; }
        public JsonTemplate[] Arguments { get; }

        public JsonTemplateCall(string name, JsonTemplate[] arguments)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }
    }
}
