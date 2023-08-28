using SeqCli.Levels;
using Serilog.Expressions;

namespace SeqCli.Syntax;

static class SeqSyntax
{
    public static CompiledExpression CompileExpression(string expression)
    {
        // Support non-Serilog level names (`@l` can't be overridden by the name resolver). At a later date,
        // we'll use the Seq.Syntax package to do this reliably (at the moment, all occurrences
        // of @l, whether referring to the property or not, will be replaced).
        var expr = expression.Replace("@l", "@Level").Replace("@Level", $"coalesce(@p['{SurrogateLevelProperty.PropertyName}'],@l)");
        return SerilogExpression.Compile(expr, nameResolver: new SeqBuiltInNameResolver());
    }
}
