namespace SeqCli.Templates.Ast
{
    class JsonTemplateNumber : JsonTemplate
    {
        public decimal Value { get; }

        public JsonTemplateNumber(decimal value)
        {
            Value = value;
        }
    }
}