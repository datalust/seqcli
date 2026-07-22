using System;
using System.Threading;
using Roastery.Util;

namespace Roastery.Model;

class MaintenanceSchedule
{
    readonly Lock _sync = new();
    
    DateTime _outageStart;
    DateTime _outageEnd;

    public MaintenanceSchedule(DateTime outageStart)
    {
        _outageStart = outageStart;
        _outageEnd = _outageStart + OutageDuration();
    }

    public bool IsUnderMaintenance()
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            if (now < _outageStart)
                return false;

            if (now < _outageEnd)
                return true;

            // Schedule the next outage for roughly a day into the future
            _outageStart = now + TimeSpan.FromHours(Distribution.Uniform(20, 28));
            _outageEnd = _outageStart + OutageDuration();
            return false;
        }
    }

    static TimeSpan OutageDuration() => TimeSpan.FromMinutes(Distribution.Uniform(35, 50));
}
