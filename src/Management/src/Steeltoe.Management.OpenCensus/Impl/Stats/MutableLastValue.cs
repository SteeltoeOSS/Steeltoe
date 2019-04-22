using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class MutableLastValue : MutableAggregation
    {
        internal double LastValue = Double.NaN;

        // TODO(songya): remove this once interval stats is completely removed.
        internal bool initialized = false;

        internal MutableLastValue() { }

        internal static MutableLastValue Create()
        {
            return new MutableLastValue();
        }

        internal override void Add(double value)
        {
            LastValue = value;

            // TODO(songya): remove this once interval stats is completely removed.
            if (!initialized)
            {
                initialized = true;
            }
        }

        internal override void Combine(MutableAggregation other, double fraction)
        {
            MutableLastValue mutable = other as MutableLastValue;
            if (mutable == null)
            {
                throw new ArgumentException("MutableLastValue expected.");
            }
            MutableLastValue otherValue = (MutableLastValue)other;
            // Assume other is always newer than this, because we combined interval buckets in time order.
            // If there's a newer value, overwrite current value.
            this.LastValue = otherValue.initialized ? otherValue.LastValue : this.LastValue;
        }

        internal override T Match<T>(Func<MutableSum, T> p0, Func<MutableCount, T> p1, Func<MutableMean, T> p2, Func<MutableDistribution, T> p3, Func<MutableLastValue, T> p4)
        {
            return p4.Invoke(this);
        }
    }
}
