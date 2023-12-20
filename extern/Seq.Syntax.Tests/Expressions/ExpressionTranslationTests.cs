using System.Collections.Generic;
using Seq.Syntax.Expressions.Compilation;
using Seq.Syntax.Expressions.Parsing;
using Seq.Syntax.Tests.Support;
using Xunit;

namespace Seq.Syntax.Tests.Expressions;

public class ExpressionTranslationTests
{
    public static IEnumerable<object[]> ExpressionEvaluationCases =>
        AsvCases.ReadCases("translation-cases.asv");

    [Theory]
    [MemberData(nameof(ExpressionEvaluationCases))]
    public void ExpressionsAreCorrectlyTranslated(string expr, string expected)
    {
        var parsed = new ExpressionParser().Parse(expr);
        var translated = ExpressionCompiler.Translate(parsed);
        var actual = translated.ToString();
        Assert.Equal(expected, actual);
    }
}