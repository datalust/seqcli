using System.Collections.Generic;
using System.Threading;

namespace Roastery.Model;

class LoadingDock
{
    readonly Lock _sync = new();
    readonly Dictionary<string, double> _pallets = new();

    public void Deliver(string blend, double kilograms)
    {
        lock (_sync)
        {
            _pallets.TryAdd(blend, 0);
            _pallets[blend] += kilograms;
        }
    }

    public IReadOnlyDictionary<string, double> Collect()
    {
        lock (_sync)
        {
            var collected = new Dictionary<string, double>(_pallets);
            _pallets.Clear();
            return collected;
        }
    }
}