using System;
using System.Linq;
using SeqCli.Levels;
using SeqCli.Syntax;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace SeqCli.Tests.Syntax;

public class SeqSyntaxTests
{
    [Fact]
    public void MessageIsExposed()
    {
        var evt = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null,
            new MessageTemplateParser().Parse("Hello"), ArraySegment<LogEventProperty>.Empty);
        
        var expr = SeqSyntax.CompileExpression("@Message");
        var result = expr(evt);
        var scalar = Assert.IsType<ScalarValue>(result);
        Assert.Equal("Hello", scalar.Value);
    }
    
    [Fact]
    public void SurrogateLevelIsExposed()
    {
        var evt = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()), new[]
        {
            new LogEventProperty(SurrogateLevelProperty.PropertyName, new ScalarValue("Warning"))
        });
        
        var expr = SeqSyntax.CompileExpression("@Level");
        var result = expr(evt);
        var scalar = Assert.IsType<ScalarValue>(result);
        Assert.Equal("Warning", scalar.Value);
    }
}
