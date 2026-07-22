using System;
using System.Collections.Generic;
using Serilog;
using Serilog.Events;

namespace Roastery.Metrics;

public class RoasteryProductionMetrics : RoasteryMetrics<RoasteryProductionMetrics.Sample>
{
    public class Sample : RoasteryMetricsSample
    {
        public record struct RoastKey(string MachineId, string RoastId, string RoastProfile);

        public record struct RoastTelemetryGauges(
            double BeanTemperature,
            double RateOfRise,
            double BurnerLevel,
            double DrumSpeed);

        public readonly Dictionary<RoastKey, RoastTelemetryGauges> RoastTelemetry = new();

        public readonly Dictionary<RoastKey, ulong> RoastBatchesStarted = new();
        public readonly Dictionary<RoastKey, ulong> RoastBatchesCompleted = new();
        public readonly Dictionary<RoastKey, ulong> RoastBatchesRejected = new();

        public readonly Dictionary<RoastKey, ExponentialHistogram> RoastDuration = new();
        public readonly Dictionary<RoastKey, ExponentialHistogram> RoastWeightLoss = new();

        public record struct AmbientKey(string Area);

        public record struct AmbientGauges(
            double AmbientTemperature,
            double AmbientRelativeHumidity,
            double AmbientBarometricPressure);

        public readonly Dictionary<AmbientKey, AmbientGauges> Ambient = new();

        public override IEnumerable<LogEvent> ToLogEvents(ILogger logger, PropertyNameMapping propertyNameMapping,
            DateTimeOffset timestamp)
        {
            foreach (var (key, gauges) in RoastTelemetry)
            {
                yield return ToLogEvent(
                    logger,
                    propertyNameMapping,
                    timestamp,
                    new Dictionary<string, object>
                    {
                        [nameof(RoastTelemetryGauges.BeanTemperature)] = new
                        {
                            kind = "Gauge",
                            unit = "Cel",
                            description = "The temperature reported by the bean mass probe."
                        },
                        [nameof(RoastTelemetryGauges.RateOfRise)] = new
                        {
                            kind = "Gauge",
                            unit = "Cel/min",
                            description = "The rate of change of the bean mass temperature."
                        },
                        [nameof(RoastTelemetryGauges.BurnerLevel)] = new
                        {
                            kind = "Gauge",
                            unit = "%",
                            description = "The burner power level applied to the roasting drum."
                        },
                        [nameof(RoastTelemetryGauges.DrumSpeed)] = new
                        {
                            kind = "Gauge",
                            unit = "rpm",
                            description = "The rotational speed of the roasting drum."
                        }
                    },
                    new Dictionary<string, object>
                    {
                        [nameof(RoastTelemetryGauges.BeanTemperature)] = gauges.BeanTemperature,
                        [nameof(RoastTelemetryGauges.RateOfRise)] = gauges.RateOfRise,
                        [nameof(RoastTelemetryGauges.BurnerLevel)] = gauges.BurnerLevel,
                        [nameof(RoastTelemetryGauges.DrumSpeed)] = gauges.DrumSpeed,
                        [nameof(key.MachineId)] = key.MachineId,
                        [nameof(key.RoastId)] = key.RoastId,
                        [nameof(key.RoastProfile)] = key.RoastProfile
                    }
                );
            }

            var counters = new (string Name, string Description, Dictionary<RoastKey, ulong> Counter)[]
            {
                (nameof(RoastBatchesStarted), "A batch of green beans was charged into a roasting machine.",
                    RoastBatchesStarted),
                (nameof(RoastBatchesCompleted), "A roast batch was dropped and passed quality control.",
                    RoastBatchesCompleted),
                (nameof(RoastBatchesRejected), "A roast batch was rejected by quality control.", RoastBatchesRejected)
            };

            foreach (var (name, description, counter) in counters)
            {
                foreach (var (key, count) in counter)
                {
                    yield return ToLogEvent(
                        logger,
                        propertyNameMapping,
                        timestamp,
                        new Dictionary<string, object>
                        {
                            [name] = new
                            {
                                kind = "Sum",
                                unit = "batch",
                                description
                            }
                        },
                        new Dictionary<string, object>
                        {
                            [name] = count,
                            [nameof(key.MachineId)] = key.MachineId,
                            [nameof(key.RoastId)] = key.RoastId,
                            [nameof(key.RoastProfile)] = key.RoastProfile
                        }
                    );
                }
            }

            var histograms =
                new (string Name, string Unit, string Description, Dictionary<RoastKey, ExponentialHistogram> Histogram)
                    []
                    {
                        (nameof(RoastDuration), "s", "The time taken to roast a batch, from charge to drop.",
                            RoastDuration),
                        (nameof(RoastWeightLoss), "%", "The percentage of green bean weight lost during roasting.",
                            RoastWeightLoss)
                    };

            foreach (var (name, unit, description, histogram) in histograms)
            {
                foreach (var (key, metric) in histogram)
                {
                    yield return ToLogEvent(
                        logger,
                        propertyNameMapping,
                        timestamp,
                        new Dictionary<string, object>
                        {
                            [name] = new
                            {
                                kind = "Exponential",
                                unit,
                                description
                            }
                        },
                        new Dictionary<string, object>
                        {
                            [name] = ToHistogramValue(metric),
                            [nameof(key.MachineId)] = key.MachineId,
                            [nameof(key.RoastId)] = key.RoastId,
                            [nameof(key.RoastProfile)] = key.RoastProfile
                        }
                    );
                }
            }

            foreach (var (key, gauges) in Ambient)
            {
                yield return ToLogEvent(
                    logger,
                    propertyNameMapping,
                    timestamp,
                    new Dictionary<string, object>
                    {
                        [nameof(AmbientGauges.AmbientTemperature)] = new
                        {
                            kind = "Gauge",
                            unit = "Cel",
                            description = "The ambient air temperature."
                        },
                        [nameof(AmbientGauges.AmbientRelativeHumidity)] = new
                        {
                            kind = "Gauge",
                            unit = "%",
                            description = "The ambient relative humidity."
                        },
                        [nameof(AmbientGauges.AmbientBarometricPressure)] = new
                        {
                            kind = "Gauge",
                            unit = "hPa",
                            description = "The ambient barometric pressure."
                        }
                    },
                    new Dictionary<string, object>
                    {
                        [nameof(AmbientGauges.AmbientTemperature)] = gauges.AmbientTemperature,
                        [nameof(AmbientGauges.AmbientRelativeHumidity)] = gauges.AmbientRelativeHumidity,
                        [nameof(AmbientGauges.AmbientBarometricPressure)] = gauges.AmbientBarometricPressure,
                        [nameof(key.Area)] = key.Area
                    }
                );
            }
        }
    }

    public void RecordRoastTelemetry(Sample.RoastKey key, Sample.RoastTelemetryGauges gauges)
    {
        Record(sample => sample.RoastTelemetry[key] = gauges);
    }

    public void RecordRoastBatchStarted(Sample.RoastKey key)
    {
        Record(sample =>
        {
            sample.RoastBatchesStarted.TryAdd(key, 0);
            sample.RoastBatchesStarted[key] += 1;
        });
    }

    public void RecordRoastBatchCompleted(Sample.RoastKey key, double durationSeconds, double weightLossPercent)
    {
        Record(sample =>
        {
            sample.RoastBatchesCompleted.TryAdd(key, 0);
            sample.RoastBatchesCompleted[key] += 1;

            if (!sample.RoastDuration.TryGetValue(key, out var duration))
            {
                duration = new ExponentialHistogram();
                sample.RoastDuration.Add(key, duration);
            }

            duration.Record(durationSeconds);

            if (!sample.RoastWeightLoss.TryGetValue(key, out var weightLoss))
            {
                weightLoss = new ExponentialHistogram();
                sample.RoastWeightLoss.Add(key, weightLoss);
            }

            weightLoss.Record(weightLossPercent);
        });
    }

    public void RecordRoastBatchRejected(Sample.RoastKey key)
    {
        Record(sample =>
        {
            sample.RoastBatchesRejected.TryAdd(key, 0);
            sample.RoastBatchesRejected[key] += 1;
        });
    }

    public void RecordAmbientConditions(Sample.AmbientKey key, Sample.AmbientGauges gauges)
    {
        Record(sample => sample.Ambient[key] = gauges);
    }
}