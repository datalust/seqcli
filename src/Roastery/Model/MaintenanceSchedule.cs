using System;
using System.Threading;
using Roastery.Util;

namespace Roastery.Model;

class MaintenanceSchedule
{
    readonly Lock _sync = new();
    DateTime _outageStart = DateTime.UtcNow + TimeSpan.FromMinutes(30);
    DateTime _outageEnd;

    public MaintenanceSchedule()
    {
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

            _outageStart = now + TimeSpan.FromHours(Distribution.Uniform(20, 28));
            _outageEnd = _outageStart + OutageDuration();
            return false;
        }
    }

    static TimeSpan OutageDuration() => TimeSpan.FromMinutes(Distribution.Uniform(35, 50));
}
