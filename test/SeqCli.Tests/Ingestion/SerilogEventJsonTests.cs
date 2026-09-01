#nullable enable
using System;
using System.Linq;
using SeqCli.Ingestion;
using SeqCli.Sample.Ingestion;
using Serilog;
using Serilog.Events;
using Xunit;

namespace SeqCli.Tests.Ingestion;

public class SerilogEventJsonTests
{
    static LogEvent CaptureEvent(Action<ILogger> log)
    {
        LogEvent? captured = null;
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new CapturingSink(evt => captured = evt))
            .CreateLogger();
        log(logger);
        return captured ?? throw new InvalidOperationException("No event was captured.");
    }

    class CapturingSink(Action<LogEvent> capture) : Serilog.Core.ILogEventSink
    {
        public void Emit(LogEvent logEvent) => capture(logEvent);
    }

    [Fact]
    public void EventFieldsMapToTheEmissionSchema()
    {
        var evt = CaptureEvent(log => log.Warning(new Exception("Boom!"), "Hello, {Name}!", "world"));
        var eventJson = SerilogEventJson.ToEventJson(evt);

        Assert.Equal(evt.Timestamp.ToString("o"), (string?)eventJson["@t"]);
        Assert.Equal("Hello, {Name}!", (string?)eventJson["@mt"]);
        Assert.Equal("Warning", (string?)eventJson["@l"]);
        Assert.StartsWith("System.Exception: Boom!", (string?)eventJson["@x"]);
        Assert.Equal("world", (string?)eventJson["Name"]);
    }

    [Fact]
    public void InformationLevelsAreOmitted()
    {
        var evt = CaptureEvent(log => log.Information("Hello"));

        Assert.False(SerilogEventJson.ToEventJson(evt).ContainsKey("@l"));
    }

    [Fact]
    public void StructuredValuesSerializeAsJson()
    {
        var evt = CaptureEvent(log => log.Information("{@Order} {Items}",
            new { Id = 7, Total = 4.5 }, new[] { "a", "b" }));
        var eventJson = SerilogEventJson.ToEventJson(evt);

        Assert.Equal(7, (int?)eventJson["Order"]!["Id"]);
        Assert.Equal(4.5, (double?)eventJson["Order"]!["Total"]);
        Assert.Equal(new[] { "a", "b" }, eventJson["Items"]!.AsArray().Select(i => (string?)i).ToArray());
    }

    [Fact]
    public void SerilogTracingSpanPropertiesAreLifted()
    {
        var start = DateTime.UtcNow.AddMilliseconds(-25);
        var evt = CaptureEvent(log => log
            .ForContext("SpanStartTimestamp", start)
            .ForContext("ParentSpanId", "8899aabbccddeeff")
            .Information("GET /orders"));
        var eventJson = SerilogEventJson.ToEventJson(evt);

        Assert.Equal(start.ToString("o"), (string?)eventJson["@st"]);
        Assert.Equal("8899aabbccddeeff", (string?)eventJson["@ps"]);
        Assert.False(eventJson.ContainsKey("SpanStartTimestamp"));
        Assert.False(eventJson.ContainsKey("ParentSpanId"));
    }

    [Fact]
    public void MetricDefinitionsProduceMetricSamples()
    {
        var evt = CaptureEvent(log => log
            .ForContext(MetricsMapping.SurrogateDefinitionsProperty, new { roasted_kg = new { unit = "kg" } }, destructureObjects: true)
            .ForContext("roasted_kg", 42.5)
            .Information("Metrics sampled"));

        Assert.True(MetricsMapping.TryGetMetricSampleJson(evt, out var eventJson));
        Assert.Equal("kg", (string?)eventJson["@d"]!["roasted_kg"]!["unit"]);
        Assert.Equal(42.5, (double?)eventJson["roasted_kg"]);
        Assert.False(eventJson.ContainsKey("@mt"));
        Assert.False(eventJson.ContainsKey("@l"));
    }

    [Fact]
    public void PlainEventsAreNotMetricSamples()
    {
        var evt = CaptureEvent(log => log.Information("Hello"));

        Assert.False(MetricsMapping.TryGetMetricSampleJson(evt, out _));
    }

    [Fact]
    public void PropertyNamesBeginningWithAtAreEscaped()
    {
        var evt = CaptureEvent(log => log.ForContext("@evil", "value").Information("Hello"));

        Assert.Equal("value", (string?)SerilogEventJson.ToEventJson(evt)["@@evil"]);
    }
}
