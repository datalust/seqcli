using SeqCli.Ingestion;
using SeqCli.Levels;
using Serilog.Formatting;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace SeqCli.Output;

static class OutputFormatter
{
    // This is the only usage of Serilog.Expressions remaining in seqcli; the upstream Seq.Syntax doesn't yet support
    // the `@sp` property, because it needs to load on older Seq installs with older Serilog versions embedded in the
    // app runner. Once we've updated it, we can switch this to a Seq.Syntax template.
    internal static ITextFormatter Json(TemplateTheme? theme) => new ExpressionTemplate(
        $"{{ {{@t, @mt, @l: coalesce({LevelMapping.SurrogateLevelProperty}, if @l = 'Information' then undefined() else @l), @x, @sp, @tr, @ps: coalesce({TraceConstants.ParentSpanIdProperty}, @ps), @st: coalesce({TraceConstants.SpanStartTimestampProperty}, @st), ..rest()}} }}\n",
        theme: theme
    );
}
