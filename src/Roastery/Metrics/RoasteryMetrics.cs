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

public class RoasteryMetrics
{
    public class Sample
    {
        /*
        Adding new metrics:

        1. Add a new key type, `TKey` for the metric's attributes using structural equality.
        2. Add a `Dictionary<TKey, TMetric>` property for the metric where `TMetric` is its collection type.
        3. Add a method to `RoasterMetrics` to add a sample to the metric for a given key.
        4. Add support in `ToLogEvents` for the new metric.
        */
        
        // `http.request.duration`: Histogram
        public record struct RequestDurationKey(string Path, int StatusCode);
        public readonly Dictionary<RequestDurationKey, ExponentialHistogram> RequestDuration = new();
        
        static readonly MessageTemplate Template = new MessageTemplateParser().Parse("Metrics sampled");

        public IEnumerable<LogEvent> ToLogEvents(ILogger logger, PropertyNameMapping propertyNameMapping, DateTimeOffset timestamp)
        {
            foreach (var (key, metric) in RequestDuration)
            {
                var metricName = "http.request.duration";
                var metricDefinition = new { kind = "Exponential", unit = "ms", description = "The time taken to fully process a request" };
                var sample = new
                {
                    http = new
                    {
                        request = new
                        {
                            duration = new
                            {
                                buckets = metric.Buckets
                                    .Select(bucket => new { midpoint = bucket.Key, count = bucket.Value }).ToArray(),
                                scale = metric.Scale,
                                min = metric.Min,
                                max = metric.Max,
                                count = metric.Total
                            }
                        }
                    },
                    path = key.Path,
                    statusCode = key.StatusCode
                };

                yield return ToLogEvent(logger, propertyNameMapping, timestamp, metricName, metricDefinition, sample);
            }
        }

        static LogEvent ToLogEvent(ILogger logger, PropertyNameMapping propertyNameMapping, DateTimeOffset timestamp, string metricName,
            object metricDefinition, object sample)
        {
            logger.BindProperty(propertyNameMapping.MetricDefinitions, new Dictionary<string, object> { { metricName, metricDefinition } }, true, out var definitionsProperty);
            logger.BindProperty(propertyNameMapping.MetricSamples, sample, true, out var sampleProperty);

            return new LogEvent(timestamp, LogEventLevel.Information, null, Template,
                [definitionsProperty!, sampleProperty!]);
        }
    }
    
    // Access to the current sample is synchronized through a lock
    // This is a simple way to implement deltas for arbitrary types
    readonly Lock _lock = new();
    Sample _current = new();

    public void RecordRequestDuration(Sample.RequestDurationKey key, double rawValue)
    {
        lock (_lock)
        {
            if (!_current.RequestDuration.TryGetValue(key, out var metric))
            {
                metric = new ExponentialHistogram();
                _current.RequestDuration.Add(key, metric);
            }

            metric.Record(rawValue);
        }
    }

    public (DateTimeOffset, Sample) Take()
    {
        var timestamp = DateTimeOffset.UtcNow;
        
        var current = new Sample();

        lock (_lock)
        {
            (current, _current) = (_current, current);
        }

        return (timestamp, current);
    }

    public static Task PeriodicSample(
        RoasteryMetrics metrics,
        TimeSpan samplingInterval,
        Func<DateTimeOffset, Sample, CancellationToken, Task> sample,
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
                    var (timestamp, current) = metrics.Take();
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
