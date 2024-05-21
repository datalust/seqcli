using System;
using System.Collections.Generic;
using Seq.Syntax.Expressions;
using SeqCli.Syntax;
using SeqCli.Util;
using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Cli.Features;

class PropertiesExpressionFeature : CommandFeature
{
    string? _expression;

    public override void Enable(OptionSet options)
    {
        options.Add(
            "properties=",
            "A Seq syntax object expression describing additional event properties",
            v => _expression = ArgumentString.Normalize(v));
    }

    public override IEnumerable<string> GetUsageErrors()
    {
        if (_expression == null) yield break;

        if (!SerilogExpression.TryCompile(_expression, null, new SeqCliNameResolver(), out _, out var error))
            yield return error;
    }

    public ILogEventEnricher? GetEnricher()
    {
        if (_expression == null) return null;

        // Spread enables the expression to overwrite existing property values, use them to compute new properties, and
        // remove them with `x: undefined()`.
        var fullExpr = $"{{..@p, ..{_expression}}}";
        var expr = SerilogExpression.Compile(fullExpr, null, new SeqCliNameResolver());
        return new PropertiesExpressionEnricher(expr);
    }

    class PropertiesExpressionEnricher : ILogEventEnricher
    {
        readonly CompiledExpression _expr;

        public PropertiesExpressionEnricher(CompiledExpression expr)
        {
            _expr = expr;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var newProps = _expr(logEvent) as StructureValue ??
                           throw new InvalidOperationException("Properties expression did not evaluate to an object value.");
            
            var missing = new HashSet<string>(logEvent.Properties.Keys);
            foreach (var prop in newProps.Properties)
            {
                missing.Remove(prop.Name);
                logEvent.AddOrUpdateProperty(prop);
            }

            foreach (var propName in missing)
            {
                logEvent.RemovePropertyIfPresent(propName);
            }
        }
    }
}