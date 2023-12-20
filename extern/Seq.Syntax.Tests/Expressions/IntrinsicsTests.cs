using System;
using Seq.Syntax.Expressions.Compilation.Linq;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Seq.Syntax.Tests.Expressions;

public class IntrinsicsTests
{
    [Fact]
    public void EventIdIsComputedFromClefProperty()
    {
        var evt = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            new MessageTemplate(ArraySegment<MessageTemplateToken>.Empty),
            new[] { new LogEventProperty("@i", new ScalarValue("a1e77001")) });

        var eventId = Intrinsics.GetEventId(evt);
        Assert.True(eventId is ScalarValue { Value: 0xa1e77001 });
    }
}