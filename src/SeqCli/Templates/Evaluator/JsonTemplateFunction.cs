namespace SeqCli.Templates.Evaluator
{
    delegate bool JsonTemplateFunction(object[] args, out object result, out string error);
}
