namespace SeqCli.Templates.Ast
{
    class JsonTemplateBoolean : JsonTemplate
    {
        public bool Value { get; }

        public JsonTemplateBoolean(bool value)
        {
            Value = value;
        }
    }
}