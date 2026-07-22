using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Events;

namespace Roastery.Metrics;

public class RoasteryWebMetrics : RoasteryMetrics<RoasteryWebMetrics.Sample>
{
    public class Sample : RoasteryMetricsSample
    {
        public record struct HttpRequestDurationKey(string Path, int StatusCode);

        public readonly Dictionary<HttpRequestDurationKey, ExponentialHistogram> HttpRequestDuration = new();

        public ulong OrderCreated;

        public ulong OrderShipped;

        public record struct StockLevelKey(string Blend);

        public readonly Dictionary<StockLevelKey, double> StockLevel = new();

        public override IEnumerable<LogEvent> ToLogEvents(ILogger logger, PropertyNameMapping propertyNameMapping,
            DateTimeOffset timestamp)
        {
            foreach (var (key, metric) in HttpRequestDuration)
            {
                yield return ToLogEvent(
                    logger,
                    propertyNameMapping,
                    timestamp,
                    new Dictionary<string, object>
                    {
                        [nameof(HttpRequestDuration)] = new
                        {
                            kind = "Exponential",
                            unit = "ms",
                            description = "The time taken to fully process a request."
                        }
                    },
                    new Dictionary<string, object>
                    {
                        [nameof(HttpRequestDuration)] = ToHistogramValue(metric),
                        [nameof(key.Path)] = key.Path,
                        [nameof(key.StatusCode)] = key.StatusCode
                    }
                );
            }

            yield return ToLogEvent(
                logger,
                propertyNameMapping,
                timestamp,
                new Dictionary<string, object>
                {
                    [nameof(OrderCreated)] = new
                    {
                        kind = "Sum",
                        unit = "order",
                        description = "An order was created."
                    },
                    [nameof(OrderShipped)] = new
                    {
                        kind = "Sum",
                        unit = "order",
                        description = "An order was shipped."
                    }
                },
                new Dictionary<string, object>
                {
                    [nameof(OrderCreated)] = OrderCreated,
                    [nameof(OrderShipped)] = OrderShipped
                }
            );

            foreach (var (key, level) in StockLevel)
            {
                yield return ToLogEvent(
                    logger,
                    propertyNameMapping,
                    timestamp,
                    new Dictionary<string, object>
                    {
                        [nameof(StockLevel)] = new
                        {
                            kind = "Gauge",
                            unit = "kg",
                            description = "The current warehouse stock of a blend, available to fulfil orders."
                        }
                    },
                    new Dictionary<string, object>
                    {
                        [nameof(StockLevel)] = level,
                        [nameof(key.Blend)] = key.Blend
                    }
                );
            }
        }
    }

    public void RecordHttpRequestDuration(Sample.HttpRequestDurationKey key, double rawValue)
    {
        Record(sample =>
        {
            if (!sample.HttpRequestDuration.TryGetValue(key, out var metric))
            {
                metric = new ExponentialHistogram();
                sample.HttpRequestDuration.Add(key, metric);
            }

            metric.Record(rawValue);
        });
    }

    public void RecordOrderCreated()
    {
        Record(sample => sample.OrderCreated += 1);
    }

    public void RecordOrderShipped()
    {
        Record(sample => sample.OrderShipped += 1);
    }

    public void RecordStockLevel(Sample.StockLevelKey key, double kilograms)
    {
        Record(sample => sample.StockLevel[key] = kilograms);
    }
}