using System.Collections.Generic;
using System.Threading;

namespace Roastery.Model;

class ProductionSchedule
{
    readonly Lock _sync = new();
    readonly Queue<string> _requestedBlends = new();

    public void Request(string blend)
    {
        lock (_sync)
        {
            if (!_requestedBlends.Contains(blend))
                _requestedBlends.Enqueue(blend);
        }
    }

    public string? TakeRequest()
    {
        lock (_sync)
        {
            return _requestedBlends.TryDequeue(out var blend) ? blend : null;
        }
    }
}
