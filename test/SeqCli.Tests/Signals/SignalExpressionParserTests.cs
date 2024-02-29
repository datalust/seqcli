using System.Collections.Generic;
using SeqCli.Signals;
using Xunit;

namespace SeqCli.Tests.Signals;

public class SignalExpressionParserTests
{
    [Theory, MemberData(nameof(_sources))]
    public void ParseSuccessfully((string, string) inputs)
    {
        var (input, expected) = inputs;

        var parsed = SignalExpressionParser.ParseExpression(input).ToString();
            
        Assert.Equal(expected, parsed);
    }
        
    public static IEnumerable<object[]> _sources = new []{
        [("signal-1 ", "signal-1")],

        [("(signal-1)", "signal-1")],

        [("signal-1 ,signal-2", "signal-1,signal-2")],

        [(" signal-1,signal-2~ signal-3", "(signal-1,signal-2)~signal-3")],

        [("signal-1,signal-2,(signal-3~signal-4)", "(signal-1,signal-2),(signal-3~signal-4)")],
            
        new object[] { ("signal-1~( (signal-2~signal-3) ,signal-4)", "signal-1~((signal-2~signal-3),signal-4)") }
    };
}
