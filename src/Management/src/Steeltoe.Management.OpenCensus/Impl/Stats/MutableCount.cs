using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class MutableCount : MutableAggregation
    {
        internal long Count { get; private set; } = 0;

        internal MutableCount() { }

 
        internal static MutableCount Create()
        {
            return new MutableCount();
        }

        internal override void Add(double value)
        {
            Count++;
        }

        internal override void Combine(MutableAggregation other, double fraction)
        {
            MutableCount mutable = other as MutableCount;
            if (mutable == null)
            {
                throw new ArgumentException("MutableCount expected.");
            }
            var result = fraction * mutable.Count;
            long rounded = (long)Math.Round(result);
            Count += rounded;
        }

        internal override T Match<T>(Func<MutableSum, T> p0, Func<MutableCount, T> p1, Func<MutableMean, T> p2, Func<MutableDistribution, T> p3, Func<MutableLastValue, T> p4)
        {
            return p1.Invoke(this);
        }
    }
}
