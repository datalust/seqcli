using Seq.Syntax.Expressions;

namespace SeqCli.Syntax;

static class SeqSyntax
{
    public static CompiledExpression CompileExpression(string expression)
    {
        return SerilogExpression.Compile(expression);
    }
}
