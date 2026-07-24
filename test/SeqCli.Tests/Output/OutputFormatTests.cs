#nullable enable
using SeqCli.Tests.Support;
using Serilog.Events;
using Xunit;
using OutputFormat = SeqCli.Output.OutputFormat;

namespace SeqCli.Tests.Output;

public class OutputFormatTests
{
    [Fact]
    public void SpanKindIsMappedFromSpans()
    {
        var evt = OutputFormat.ToSerilogEvent(Some.MakeEvent(e =>
        {
            e.Properties = [];
            e.Start = "2024-01-01T00:00:00.0000000Z";
            e.SpanKind = "Server";
        }));

        Assert.Equal("Server", Assert.IsType<ScalarValue>(evt.Properties["@sk"]).Value);
    }

    [Fact]
    public void SpanKindIsOmittedFromEventsThatAreNotSpans()
    {
        // The events API reports a span kind for every event, defaulting to `Internal`.
        var evt = OutputFormat.ToSerilogEvent(Some.MakeEvent(e =>
        {
            e.Properties = [];
            e.SpanKind = "Internal";
        }));

        Assert.DoesNotContain("@sk", evt.Properties.Keys);
    }
}
