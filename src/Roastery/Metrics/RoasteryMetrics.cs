using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Serilog.Parsing;

namespace Roastery.Metrics;

/*
Adding new metrics:

1. Pick the metrics class for the producing application: `RoasteryWebMetrics` for the web frontend,
   `RoasteryProductionMetrics` for the roasting plant.
2. Add a new key type, `TKey` for the metric's attributes using structural equality. Keys can carry
   arbitrary, high-cardinality attributes: each distinct key only lives for a single sampling
   interval.
3. Add a `Dictionary<TKey, TMetric>` property to the class's `Sample` where `TMetric` is the
   metric's collection type. Counters and histograms are deltas — they accumulate within a sampling
   interval and are reset when the sample is taken. Gauges store the raw sampled value,
   last-write-wins.
4. Add a method to the metrics class to add a sample to the metric for a given key.
5. Add support in the `Sample`'s `ToLogEvents` for the new metric.
*/

public abstract class RoasteryMetrics<TSample>
    where TSample : RoasteryMetricsSample, new()
{
    readonly Lock _lock = new();
    TSample _current = new();

    protected void Record(Action<TSample> record)
    {
        lock (_lock)
        {
            record(_current);
        }
    }

    public (DateTimeOffset, TSample) Take()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var current = new TSample();

        lock (_lock)
        {
            (current, _current) = (_current, current);
        }

        return (timestamp, current);
    }

    public Task PeriodicSample(
        TimeSpan samplingInterval,
        Func<DateTimeOffset, TSample, CancellationToken, Task> sample,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            var waitFor = samplingInterval;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(waitFor, cancellationToken);

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var (timestamp, current) = Take();
                    await sample(timestamp, current, cancellationToken);
                }
                catch
                {
                    // Ignored
                }

                // Account for the time taken to produce the sample when computing
                // the next interval to wait for
                var elapsed = stopwatch.Elapsed;
                waitFor = elapsed < samplingInterval ? samplingInterval - stopwatch.Elapsed : samplingInterval;
            }
        }, cancellationToken);
    }
}

public abstract class RoasteryMetricsSample
{
    static readonly MessageTemplate Template = new MessageTemplateParser().Parse("Metrics sampled");

    public abstract IEnumerable<LogEvent> ToLogEvents(ILogger logger, PropertyNameMapping propertyNameMapping,
        DateTimeOffset timestamp);

    protected static object ToHistogramValue(ExponentialHistogram metric)
    {
        return new
        {
            buckets = metric.Buckets
                .Select(bucket => new { midpoint = bucket.Key, count = bucket.Value }).ToArray(),
            scale = metric.Scale,
            min = metric.Min,
            max = metric.Max,
            count = metric.Total
        };
    }

    protected static LogEvent ToLogEvent(ILogger logger, PropertyNameMapping propertyNameMapping,
        DateTimeOffset timestamp, Dictionary<string, object> definitions, Dictionary<string, object> samples)
    {
        var properties = new List<LogEventProperty>();

        if (logger.BindProperty(propertyNameMapping.MetricDefinitions, definitions, true,
                out var definitionsProperty))
        {
            properties.Add(definitionsProperty);
        }

        foreach (var (key, value) in samples)
        {
            if (logger.BindProperty(key, value, true, out var sample))
            {
                properties.Add(sample);
            }
        }

        return new LogEvent(timestamp, LogEventLevel.Information, null, Template, properties);
    }
}