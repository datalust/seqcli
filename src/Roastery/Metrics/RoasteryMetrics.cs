using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Roastery.Metrics;

public class RoasteryMetrics
{
    readonly Lock _lock = new();

    // Request Duration: Histogram
    public record struct RequestDurationKey(string Path, int StatusCode);
    Dictionary<RequestDurationKey, ExponentialHistogram> _requestDuration = new();

    public record struct Sample(
        Dictionary<RequestDurationKey, ExponentialHistogram> RequestDuration
    )
    {
        public IEnumerable<dynamic> ToEvents()
        {
            throw new NotImplementedException();
        }
    }

    public void RecordRequestDuration(RequestDurationKey key, double rawValue)
    {
        lock (_lock)
        {
            if (!_requestDuration.TryGetValue(key, out var metric))
            {
                metric = new ExponentialHistogram();
                _requestDuration.Add(key, metric);
            }
            
            metric.Record(rawValue);
        }
    }

    public Sample Take()
    {
        var requestDuration = new Dictionary<RequestDurationKey, ExponentialHistogram>();

        lock (_lock)
        {
            (requestDuration, _requestDuration) = (_requestDuration, requestDuration);
        }

        return new Sample(requestDuration);
    }

    public static Task PeriodicSample(
        RoasteryMetrics metrics,
        TimeSpan samplingInterval,
        Func<Sample, CancellationToken, Task> sample,
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
                    await sample(metrics.Take(), cancellationToken);
                }
                catch
                {
                    // Ignored
                }

                var elapsed = stopwatch.Elapsed;
                waitFor = elapsed < samplingInterval ? samplingInterval - stopwatch.Elapsed : samplingInterval;
            }
        }, cancellationToken);
    }
}
