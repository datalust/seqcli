﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Seq.Syntax.Expressions;
using Seq.Syntax.Expressions.Runtime;
using Seq.Syntax.Tests.Support;
using Serilog.Events;
using Xunit;

namespace Seq.Syntax.Tests.Expressions;

public class NameResolverTests
{
    public static LogEventPropertyValue? Magic(LogEventPropertyValue? number)
    {
        if (!Coerce.Numeric(number, out var num))
            return null;

        return new ScalarValue(num + 42);
    }

    public static LogEventPropertyValue? SecretWordAt(string word, LogEventPropertyValue? index)
    {
        if (!Coerce.Numeric(index, out var i))
            return null;

        return new ScalarValue(word[(int)i].ToString());
    }

    class SecretWordResolver : NameResolver
    {
        readonly NameResolver _inner;
        readonly string _word;

        public SecretWordResolver(NameResolver inner, string word)
        {
            _inner = inner;
            _word = word;
        }

        public override bool TryResolveFunctionName(string name, [MaybeNullWhen(false)] out MethodInfo implementation)
            => _inner.TryResolveFunctionName(name, out implementation);

        public override bool TryBindFunctionParameter(ParameterInfo parameter, [MaybeNullWhen(false)] out object boundValue)
        {
            if (parameter.ParameterType == typeof(string))
            {
                boundValue = _word;
                return true;
            }

            boundValue = null;
            return false;
        }
    }

    [Fact]
    public void UserDefinedFunctionsAreCallableInExpressions()
    {
        var expr = SerilogExpression.Compile(
            "magic(10) + 3 = 55",
            nameResolver: new StaticMemberNameResolver(typeof(NameResolverTests)));
        Assert.True(Coerce.IsTrue(expr(Some.InformationEvent())));
    }

    [Fact]
    public void UserDefinedFunctionsCanReceiveUserProvidedParameters()
    {
        var expr = SerilogExpression.Compile(
            "SecretWordAt(1) = 'e'",
            nameResolver: new SecretWordResolver(new StaticMemberNameResolver(typeof(NameResolverTests)), "hello"));
        Assert.True(Coerce.IsTrue(expr(Some.InformationEvent())));
    }
}