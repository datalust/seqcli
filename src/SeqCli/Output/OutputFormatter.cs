using Serilog.Formatting;
using Serilog.Templates;

namespace SeqCli.Output;

static class OutputFormatter
{
    internal static readonly ITextFormatter Json = new ExpressionTemplate(
        "{ {@t, @mt, @l: if @l = 'Information' then undefined() else @l, @x, @sp, @tr, @ps, @st, ..rest()} }\n"
    );
}