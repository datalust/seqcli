using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Metrics;
using Roastery.Model;
using Roastery.Util;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using SerilogTracing;

namespace Roastery.Agents;

class RoastingMachine : Agent
{
    static readonly RoastProfile[] Profiles =
    [
        new("1 AM Medium Roast", DropTemperatureCelsius: 210, FinalBurnerLevelPercent: 50, TypicalWeightLossPercent: 13.5),
        new("Rocket Ship Dark Roast", DropTemperatureCelsius: 224, FinalBurnerLevelPercent: 58, TypicalWeightLossPercent: 16.5)
    ];

    // The bean probe reading rises toward the drum environment temperature at a rate
    // proportional to the difference between them; the operator steps the burner down
    // as the roast approaches its target, producing the characteristic declining
    // rate-of-rise curve.
    const double TurningPointCelsius = 95;
    const double InitialBurnerLevelPercent = 90;
    const double HeatTransferPerMinute = 0.2;
    const int TickMilliseconds = 4000;

    class RoastState
    {
        public double BeanTemperature;
        public double RateOfRise;
        public double BurnerLevel = InitialBurnerLevelPercent;
        public double DrumSpeed = 64;
        public bool PassedTurningPoint;
        public double? FaultAtTemperature;
        public int FaultTicksRemaining;
        public bool HadFault;
    }

    readonly ILogger _logger;
    readonly RoasteryProductionMetrics _metrics;
    readonly LoadingDock _loadingDock;
    readonly ProductionSchedule _productionSchedule;
    readonly MaintenanceSchedule _maintenanceSchedule;
    readonly string _machineId;
    bool _offlineForServicing;

    // Each machine's temperature calibration drifts a little; this shows up as a
    // per-machine skew in roast durations and weight loss
    readonly double _calibrationBiasCelsius = Distribution.Uniform(0, 6) - 3;

    public RoastingMachine(ILogger logger, RoasteryProductionMetrics metrics, LoadingDock loadingDock,
        ProductionSchedule productionSchedule, MaintenanceSchedule maintenanceSchedule, string machineId)
        : base(20000)
    {
        _logger = logger.ForContext<RoastingMachine>();
        _metrics = metrics;
        _loadingDock = loadingDock;
        _productionSchedule = productionSchedule;
        _maintenanceSchedule = maintenanceSchedule;
        _machineId = machineId;
    }

    protected override IEnumerable<Behavior> GetBehaviors()
    {
        yield return RoastBatch;
    }

    async Task RoastBatch(CancellationToken cancellationToken)
    {
        if (_maintenanceSchedule.IsUnderMaintenance())
        {
            if (!_offlineForServicing)
            {
                _offlineForServicing = true;
                _logger.Warning("Roasting machine {MachineId} is offline: the afterburner exhaust system requires servicing; roasting is suspended",
                    _machineId);
            }

            return;
        }

        if (_offlineForServicing)
        {
            _offlineForServicing = false;
            _logger.Information("Servicing complete; roasting machine {MachineId} is back online", _machineId);
        }

        // The machine sits idle until the warehouse requests more stock of a blend
        var requestedBlend = _productionSchedule.TakeRequest();
        if (requestedBlend == null)
            return;

        var profile = Profiles.FirstOrDefault(p => p.Name == requestedBlend);
        if (profile == null)
            return;

        var roastId = "roast-" + Guid.NewGuid().ToString("n")[..8];
        var key = new RoasteryProductionMetrics.Sample.RoastKey(_machineId, roastId, profile.Name);

        using var _ = LogContext.PushProperty("MachineId", _machineId);
        using var __ = LogContext.PushProperty("RoastId", roastId);
        using var activity = _logger.StartActivity("Roast {RoastProfile} batch {RoastId} on machine {MachineId}",
            profile.Name, roastId, _machineId);

        var greenWeightKilograms = Math.Round(Distribution.Uniform(80, 110), 1);
        var firstCrackTemperature = 194 + Distribution.Uniform(0, 4);

        var state = new RoastState
        {
            BeanTemperature = 190 + Distribution.Uniform(0, 6) - 3 + _calibrationBiasCelsius,
            FaultAtTemperature = Distribution.OnceIn(8) ? Distribution.Uniform(120, 190) : null
        };

        _metrics.RecordRoastBatchStarted(key);
        _logger.Information("Charged {GreenWeightKilograms}kg of green beans for {RoastProfile} at drum temperature {ChargeTemperature:F1}°C",
            greenWeightKilograms, profile.Name, state.BeanTemperature);

        var roastTiming = Stopwatch.StartNew();

        await AdvancePhaseAsync("Drying", key, profile, state, 150, cancellationToken);
        await AdvancePhaseAsync("Browning", key, profile, state, firstCrackTemperature, cancellationToken);

        _logger.Information("First crack detected at {BeanTemperature:F1}°C, {ElapsedSeconds:F0}s into the roast",
            state.BeanTemperature, roastTiming.Elapsed.TotalSeconds);

        await AdvancePhaseAsync("Development", key, profile, state, profile.DropTemperatureCelsius, cancellationToken);

        roastTiming.Stop();
        var dropTemperature = state.BeanTemperature;

        using (_logger.StartActivity("Cooling batch {RoastId}", roastId))
        {
            state.BurnerLevel = 0;
            state.RateOfRise = 0;
            for (var i = 0; i < 4; ++i)
            {
                await Task.Delay(TickMilliseconds, cancellationToken);
                state.BeanTemperature += (45 - state.BeanTemperature) * 0.4;
                RecordTelemetry(key, state);
            }
        }

        var durationSeconds = Math.Round(roastTiming.Elapsed.TotalSeconds, 1);
        activity.AddProperty("RoastDurationSeconds", durationSeconds);

        if (state.HadFault && Distribution.OnceIn(3) || Distribution.OnceIn(70))
        {
            _metrics.RecordRoastBatchRejected(key);
            _logger.Error("Batch {RoastId} rejected by quality control: uneven development following an unstable roast curve", roastId);
            activity.Complete(LogEventLevel.Error);
            return;
        }

        var weightLossPercent = Math.Round(profile.TypicalWeightLossPercent + Distribution.Uniform(0, 2.5) - 1.25, 1);
        var roastedWeightKilograms = Math.Round(greenWeightKilograms * (1 - weightLossPercent / 100), 1);

        _metrics.RecordRoastBatchCompleted(key, durationSeconds, weightLossPercent);
        _loadingDock.Deliver(profile.Name, roastedWeightKilograms);
        _logger.Information("Dropped {GreenWeightKilograms}kg batch of {RoastProfile} at {DropTemperature:F1}°C after {RoastDurationSeconds}s with {WeightLossPercent}% weight loss; {RoastedWeightKilograms}kg sent to the loading dock",
            greenWeightKilograms, profile.Name, dropTemperature, durationSeconds, weightLossPercent, roastedWeightKilograms);
    }

    async Task AdvancePhaseAsync(
        string phaseName,
        RoasteryProductionMetrics.Sample.RoastKey key,
        RoastProfile profile,
        RoastState state,
        double targetTemperature,
        CancellationToken cancellationToken)
    {
        using var phase = _logger.StartActivity("{RoastPhase} phase of batch {RoastId}", phaseName, key.RoastId);

        while (state.BeanTemperature < targetTemperature || !state.PassedTurningPoint)
        {
            await Task.Delay((int)Distribution.Uniform(TickMilliseconds - 500, TickMilliseconds + 500), cancellationToken);

            if (state is { PassedTurningPoint: true, FaultAtTemperature: not null } &&
                state.BeanTemperature >= state.FaultAtTemperature)
            {
                state.FaultAtTemperature = null;
                state.FaultTicksRemaining = (int)Distribution.Uniform(4, 8);
                state.HadFault = true;
                _logger.Warning("Burner flame-out detected on {MachineId} during {RoastPhase}; rate of rise is crashing",
                    _machineId, phaseName);
            }

            var wasFaulted = state.FaultTicksRemaining > 0;
            Advance(state, profile);
            if (wasFaulted && state.FaultTicksRemaining == 0)
                _logger.Information("Burner reignited on {MachineId}; roast curve recovering", _machineId);

            RecordTelemetry(key, state);
        }

        phase.AddProperty("BeanTemperature", Math.Round(state.BeanTemperature, 1));
    }

    void Advance(RoastState state, RoastProfile profile)
    {
        if (!state.PassedTurningPoint)
        {
            // The probe reading falls rapidly toward the turning point as the cold beans
            // absorb the drum's stored heat
            state.RateOfRise = -2.2 * (state.BeanTemperature - 88) + Distribution.Uniform(0, 8) - 4;
            if (state.BeanTemperature <= TurningPointCelsius + 2)
            {
                state.PassedTurningPoint = true;
                state.RateOfRise = 2;
            }
        }
        else
        {
            var progress = Math.Clamp(
                (state.BeanTemperature - TurningPointCelsius) / (profile.DropTemperatureCelsius - TurningPointCelsius), 0, 1);
            var targetBurnerLevel = state.FaultTicksRemaining > 0
                ? 15
                : InitialBurnerLevelPercent - (InitialBurnerLevelPercent - profile.FinalBurnerLevelPercent) * progress;

            state.BurnerLevel = Math.Clamp(
                state.BurnerLevel + (targetBurnerLevel - state.BurnerLevel) * 0.5 + Distribution.Uniform(0, 4) - 2, 10, 100);

            var drumEnvironmentTemperature = 175 + state.BurnerLevel * 1.45 + _calibrationBiasCelsius;
            state.RateOfRise = Math.Max(
                HeatTransferPerMinute * (drumEnvironmentTemperature - state.BeanTemperature) + Distribution.Uniform(0, 1.5) - 0.75,
                0.3);

            if (state.FaultTicksRemaining > 0)
                state.FaultTicksRemaining -= 1;
        }

        state.BeanTemperature += state.RateOfRise * TickMilliseconds / 60000.0;
        state.DrumSpeed = 64 + Distribution.Uniform(0, 2) - 1;
    }

    void RecordTelemetry(RoasteryProductionMetrics.Sample.RoastKey key, RoastState state)
    {
        _metrics.RecordRoastTelemetry(key, new RoasteryProductionMetrics.Sample.RoastTelemetryGauges(
            Math.Round(state.BeanTemperature, 1),
            Math.Round(state.RateOfRise, 1),
            Math.Round(state.BurnerLevel, 1),
            Math.Round(state.DrumSpeed, 1)));
    }
}
