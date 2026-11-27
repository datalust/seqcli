using System;
using System.Collections.Generic;

namespace Roastery.Metrics;

public class ExponentialHistogram
{
    public ExponentialHistogram(int initialScale = 20, int targetBuckets = 160)
    {
        _scale = initialScale;
        _targetBuckets = targetBuckets;
        _buckets = new Dictionary<double, ulong>();
    }

    readonly int _targetBuckets;

    int _scale;
    Dictionary<double, ulong> _buckets;

    double _min = double.PositiveInfinity;
    double _max = double.NegativeInfinity;
    double _sum;
    ulong _total;

    public void Record(double rawValue)
    {
        _min = Math.Min(_min, rawValue);
        _max = Math.Max(_max, rawValue);
        _sum += rawValue;
        _total += 1;

        var midpoint = Midpoint(_scale, rawValue);
        _buckets.TryAdd(midpoint, 0);
        _buckets[midpoint] += 1;

        if (_buckets.Count <= _targetBuckets) return;

        // Rescale
        var newScale = _scale - 1;
        var newBuckets = new Dictionary<double, ulong>();

        foreach (var (oldMidpoint, count) in _buckets)
        {
            var newMidpoint = Midpoint(newScale, oldMidpoint);
            newBuckets.TryAdd(newMidpoint, 0);
            newBuckets[newMidpoint] += count;
        }

        _buckets = newBuckets;
        _scale = newScale;
    }

    static double Midpoint(int scale, double rawValue)
    {
        // Zero has no logarithm; it gets a bucket of its own rather than an infinite midpoint.
        if (rawValue == 0)
            return 0;

        // Bucket `index` covers `(gamma^(index - 1), gamma^index]`, where `gamma = 2^(2^-scale)`.
        var bucketsPerPowerOfTwo = Math.Pow(2d, scale);
        var index = Math.Ceiling(Math.Log2(Math.Abs(rawValue)) * bucketsPerPowerOfTwo);

        var lower = Math.Pow(2d, (index - 1) / bucketsPerPowerOfTwo);
        var upper = Math.Pow(2d, index / bucketsPerPowerOfTwo);

        return Math.Sign(rawValue) * (lower + upper) / 2;
    }

    public IReadOnlyDictionary<double, ulong> Buckets => _buckets;
    public int Scale => _scale;

    public double Min => _min;
    public double Max => _max;
    public double Sum => _sum;
    public ulong Total => _total;
}