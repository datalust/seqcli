using System;
using System.Collections.Generic;
using System.Globalization;
using Seq.Syntax.Expressions;
using Seq.Syntax.Expressions.Runtime;
using Seq.Syntax.Tests.Support;
using Serilog.Events;
using Xunit;

namespace Seq.Syntax.Tests.Expressions;

public class ExpressionEvaluationTests
{
    public static IEnumerable<object[]> ExpressionEvaluationCases =>
        AsvCases.ReadCases("expression-evaluation-cases.asv");

    [Theory]
    [MemberData(nameof(ExpressionEvaluationCases))]
    public void ExpressionsAreCorrectlyEvaluated(string expr, string result)
    {
        var evt = Some.InformationEvent();

        evt.AddPropertyIfAbsent(
            new LogEventProperty("User", new StructureValue(new[]
            {
                new LogEventProperty("Id", new ScalarValue(42)),
                new LogEventProperty("Name", new ScalarValue("nblumhardt")),
            })));
        
        evt.AddPropertyIfAbsent(new LogEventProperty("@st", new ScalarValue((evt.Timestamp - TimeSpan.FromMinutes(10)).ToString("o"))));

        var frFr = CultureInfo.GetCultureInfoByIetfLanguageTag("fr-FR");
        var actual = SerilogExpression.Compile(expr, formatProvider: frFr)(evt);
        var expected = SerilogExpression.Compile(result)(evt);

        if (expected is null)
        {
            Assert.True(actual is null, $"Expected value: undefined{Environment.NewLine}Actual value: {Display(actual)}");
        }
        else
        {
            Assert.True(Coerce.IsTrue(RuntimeOperators._Internal_Equal(StringComparison.OrdinalIgnoreCase, actual, expected)), $"Expected value: {Display(expected)}{Environment.NewLine}Actual value: {Display(actual)}");
        }
    }

    static string Display(LogEventPropertyValue? value)
    {
        if (value == null)
            return "undefined";

        return value.ToString();
    }
}