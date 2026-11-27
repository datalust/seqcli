using System;
using System.Linq;
using Roastery.Metrics;
using Xunit;

namespace SeqCli.Tests.Sample;

public class ExponentialHistogramTests
{
    [Theory]
    [InlineData(0d)]
    [InlineData(0.001)]
    [InlineData(1d)]
    [InlineData(-3.5)]
    [InlineData(1e12)]
    public void MidpointsAreFinite(double value)
    {
        var histogram = new ExponentialHistogram();
        histogram.Record(value);

        var (midpoint, count) = Assert.Single(histogram.Buckets);
        Assert.True(double.IsFinite(midpoint));
        Assert.Equal(1ul, count);
    }

    [Fact]
    public void MidpointsAreCloseToRecordedValues()
    {
        var histogram = new ExponentialHistogram(initialScale: 3);
        histogram.Record(100);

        var midpoint = Assert.Single(histogram.Buckets).Key;
        Assert.InRange(midpoint, 100 / Math.Pow(2, 1 / 8d), 100 * Math.Pow(2, 1 / 8d));
    }

    [Fact]
    public void RescalingKeepsBucketCountBoundedAndPreservesTotals()
    {
        const int targetBuckets = 20;
        var histogram = new ExponentialHistogram(targetBuckets: targetBuckets);

        for (var i = 0; i < 10_000; ++i)
            histogram.Record(i);

        Assert.InRange(histogram.Buckets.Count, 1, targetBuckets);
        Assert.InRange(histogram.Scale, 0, 20);
        Assert.All(histogram.Buckets.Keys, midpoint => Assert.True(double.IsFinite(midpoint)));
        Assert.Equal(10_000ul, histogram.Buckets.Values.Aggregate(0ul, (sum, count) => sum + count));
    }
}
