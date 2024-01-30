using SeqCli.Ingestion;
using SeqCli.Levels;
using Serilog.Formatting;
using Seq.Syntax.Templates;

namespace SeqCli.Output;

static class OutputFormatter
{
    internal static readonly ITextFormatter Json = new ExpressionTemplate(
        $"{{ {{@t, @mt, @l: coalesce({LevelMapping.SurrogateLevelProperty}, if @l = 'Information' then undefined() else @l), @x, @sp, @tr, @ps: coalesce({TraceConstants.ParentSpanIdProperty}, @ps), @st: coalesce({TraceConstants.SpanStartTimestampProperty}, @st), ..rest()}} }}\n"
    );
}