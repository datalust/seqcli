using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Roastery.Util;

static class Distribution
{
    static readonly Random Rng = new(DateTime.Now.Millisecond);

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static double Uniform(double min, double max)
    {
        if (min < 0)
            throw new ArgumentOutOfRangeException(nameof(min));
            
        if (max < min || max - min < double.Epsilon)
            throw new ArgumentOutOfRangeException(nameof(max));
            
        return min + Rng.NextDouble() * (max - min);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static T Uniform<T>(IList<T> items)
    {
        var i = (int) Uniform(0, items.Count);
        return items[i];
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static double Uniform()
    {
        return Rng.NextDouble();
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static bool OnceIn(int times)
    {
        return Uniform(0, times) < 1.0;
    }
}