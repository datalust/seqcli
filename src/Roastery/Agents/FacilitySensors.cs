using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Metrics;
using Roastery.Util;
using Serilog;

namespace Roastery.Agents;

class FacilitySensors : Agent
{
    class AreaState
    {
        public AreaState(string area, double baseTemperature, double relativeHumidity)
        {
            Area = area;
            BaseTemperature = baseTemperature;
            RelativeHumidity = relativeHumidity;
        }

        public string Area { get; }
        public double BaseTemperature { get; }
        public double RelativeHumidity { get; set; }
    }

    readonly ILogger _logger;
    readonly RoasteryProductionMetrics _metrics;

    readonly AreaState[] _areas =
    [
        new("Roasting Floor", baseTemperature: 27, relativeHumidity: 52),
        new("Green Bean Warehouse", baseTemperature: 19, relativeHumidity: 60)
    ];

    double _barometricPressure = 1015;

    public FacilitySensors(ILogger logger, RoasteryProductionMetrics metrics)
        : base(6000)
    {
        _logger = logger.ForContext<FacilitySensors>();
        _metrics = metrics;
    }

    protected override IEnumerable<Behavior> GetBehaviors()
    {
        yield return SampleEnvironment;
    }

    Task SampleEnvironment(CancellationToken cancellationToken)
    {
        // Facility temperatures peak mid-afternoon and bottom out overnight
        var hour = DateTime.Now.TimeOfDay.TotalHours;
        var diurnalSwing = 3.5 * Math.Sin((hour - 9.0) / 24.0 * 2.0 * Math.PI);

        _barometricPressure = Math.Clamp(_barometricPressure + Distribution.Uniform(0, 0.6) - 0.3, 990, 1035);

        foreach (var area in _areas)
        {
            var temperature = area.BaseTemperature + diurnalSwing + Distribution.Uniform(0, 0.8) - 0.4;
            area.RelativeHumidity = Math.Clamp(
                area.RelativeHumidity + (58 - area.RelativeHumidity) * 0.02 + Distribution.Uniform(0, 2.4) - 1.2, 35, 85);

            _metrics.RecordAmbientConditions(
                new RoasteryProductionMetrics.Sample.AmbientKey(area.Area),
                new RoasteryProductionMetrics.Sample.AmbientGauges(
                    Math.Round(temperature, 1),
                    Math.Round(area.RelativeHumidity, 1),
                    Math.Round(_barometricPressure, 1)));

            if (area.RelativeHumidity > 75 && Distribution.OnceIn(10))
                _logger.Warning("Relative humidity in the {Area} has reached {RelativeHumidity:F0}%; green coffee should be stored below 65% RH",
                    area.Area, area.RelativeHumidity);
        }

        return Task.CompletedTask;
    }
}
