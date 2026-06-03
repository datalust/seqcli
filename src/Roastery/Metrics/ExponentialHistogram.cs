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

    double _min;
    double _max;
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
            var newMidpoint = Midpoint(_scale, oldMidpoint);
            newBuckets.TryAdd(newMidpoint, 0);
            newBuckets[newMidpoint] += count;
        }

        _buckets = newBuckets;
        _scale = newScale;
    }

    static double Midpoint(int scale, double rawValue)
    {
        var gamma = Math.Pow(2d, Math.Pow(2d, -scale));
        var index = Math.Abs(Math.Log(rawValue, gamma));
        
        return (Math.Pow(gamma, index - 1) + Math.Pow(gamma, index)) / 2;
    }

    public IReadOnlyDictionary<double, ulong> Buckets => _buckets;
    public int Scale => _scale;
    
    public double Min => _min;
    public double Max => _max;
    public double Sum => _sum;
    public ulong Total => _total;
}