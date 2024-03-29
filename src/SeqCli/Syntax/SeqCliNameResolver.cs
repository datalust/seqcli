using System.Diagnostics.CodeAnalysis;
using Seq.Syntax.Expressions;

namespace SeqCli.Syntax;

class SeqCliNameResolver: NameResolver
{
    public override bool TryResolveBuiltInPropertyName(string alias, [MaybeNullWhen(false)] out string target)
    {
        switch (alias)
        {
            case "@l":
                target = "coalesce(SeqCliOriginalLevel, @l)";
                return true;
            default:
                target = null;
                return false;
        }
    }
}
