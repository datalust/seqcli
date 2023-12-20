using System;
using System.IO;
using System.Threading;
using Seq.Syntax.Expressions;
using Seq.Syntax.Expressions.Runtime;
using Seq.Syntax.Templates.Compilation;
using Serilog.Events;
using Serilog.Parsing;

namespace Seq.Syntax.Templates.Encoding;

class EscapableEncodedCompiledFormattedExpression : CompiledTemplate
{
    static int _nextSubstituteLocalNameSuffix;
    readonly string _substituteLocalName = $"%sub{Interlocked.Increment(ref _nextSubstituteLocalNameSuffix)}";
    readonly Evaluatable _expression;
    readonly TemplateOutputEncoder _encoder;
    readonly CompiledFormattedExpression _inner;

    public EscapableEncodedCompiledFormattedExpression(Evaluatable expression, string? format, Alignment? alignment, IFormatProvider? formatProvider, TemplateOutputEncoder encoder)
    {
        _expression = expression;
        _encoder = encoder;
        _inner = new CompiledFormattedExpression(GetSubstituteLocalValue, format, alignment, formatProvider);
    }

    LogEventPropertyValue? GetSubstituteLocalValue(EvaluationContext context)
    {
        return Locals.TryGetValue(context.Locals, _substituteLocalName, out var computed)
            ? computed
            : null;
    }

    public override void Evaluate(EvaluationContext ctx, TextWriter output)
    {
        var value = _expression(ctx);
            
        if (value is ScalarValue { Value: PreEncodedValue pv })
        {
            var rawContext = pv.Inner == null ?
                new EvaluationContext(ctx.LogEvent) :
                new EvaluationContext(ctx.LogEvent, Locals.Set(ctx.Locals, _substituteLocalName, pv.Inner));
            _inner.Evaluate(rawContext, output);
            return;
        }

        var buffer = new StringWriter(output.FormatProvider);
            
        var bufferedContext = value == null
            ? ctx
            : new EvaluationContext(ctx.LogEvent, Locals.Set(ctx.Locals, _substituteLocalName, value));

        _inner.Evaluate(bufferedContext, buffer);
        var encoded = _encoder.Encode(buffer.ToString());
        output.Write(encoded);
    }
}