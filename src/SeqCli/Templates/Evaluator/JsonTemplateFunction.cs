using SeqCli.Templates.Ast;

namespace SeqCli.Templates.Evaluator
{
    delegate bool JsonTemplateFunction(JsonTemplate[] args, out JsonTemplate result, out string error);
}
