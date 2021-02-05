using System;
using System.Collections.Generic;
using System.Threading;

namespace Roastery.Util
{
    class Distribution
    {
        static int _nextSeed = DateTime.Now.Millisecond;
        readonly Random _rng = new(Interlocked.Increment(ref _nextSeed));

        public double Uniform(double min, double max)
        {
            if (min < 0)
                throw new ArgumentOutOfRangeException(nameof(min));
            
            if (max < min || max - min < double.Epsilon)
                throw new ArgumentOutOfRangeException(nameof(max));
            
            return min + _rng.NextDouble() * (max - min);
        }

        public T Uniform<T>(IList<T> items)
        {
            var i = (int) Uniform(0, items.Count);
            return items[i];
        }

        public double Uniform()
        {
            return _rng.NextDouble();
        }

        public bool OnceIn(int times)
        {
            return Uniform(0, (double) times) < 1.0;
        }
    }
}
