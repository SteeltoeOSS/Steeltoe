using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    internal sealed class MutableSum : MutableAggregation
    {
        internal double Sum { get; private set; } = 0.0;

        internal MutableSum() { }

 
        internal static MutableSum Create()
        {
            return new MutableSum();
        }

        internal override void Add(double value)
        {
            Sum += value;
        }

        internal override void Combine(MutableAggregation other, double fraction)
        {
            MutableSum mutable = other as MutableSum;
            if (mutable == null)
            {
                throw new ArgumentException("MutableSum expected.");
            }

            this.Sum += fraction * mutable.Sum;
        }
        internal override T Match<T>(Func<MutableSum, T> p0, Func<MutableCount, T> p1, Func<MutableMean, T> p2, Func<MutableDistribution, T> p3)
        {
            return p0.Invoke(this);
        }
    }
}
