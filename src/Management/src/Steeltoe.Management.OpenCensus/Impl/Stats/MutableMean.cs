using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class MutableMean : MutableAggregation
    {
        internal double Sum { get; set; } = 0.0;
        internal long Count { get; set; } = 0;
        internal double Mean
        {
            get
            {
                return Count == 0 ? 0 : Sum / Count;
            }
        }
        internal double Min { get; set; } = Double.MaxValue;
        internal double Max { get; set; } = Double.MinValue;

        internal MutableMean() { }

        internal static MutableMean Create()
        {
            return new MutableMean();
        }

        internal override void Add(double value)
        {
            Count++;
            Sum += value;
            if (value < Min)
            {
                Min = value;
            }
            if (value > Max)
            {
                Max = value;
            }
        }

        internal override void Combine(MutableAggregation other, double fraction)
        {
            MutableMean mutable = other as MutableMean;
            if (mutable == null)
            {
                throw new ArgumentException("MutableMean expected.");
            }
            var result = fraction * mutable.Count;
            long rounded = (long)Math.Round(result);
            Count += rounded;

            this.Sum += mutable.Sum * fraction;

            if (mutable.Min < this.Min)
            {
                this.Min = mutable.Min;
            }
            if (mutable.Max > this.Max)
            {
                this.Max = mutable.Max;
            }

        }
        internal override T Match<T>(Func<MutableSum, T> p0, Func<MutableCount, T> p1, Func<MutableMean, T> p2, Func<MutableDistribution, T> p3, Func<MutableLastValue, T> p4)
        {
            return p2.Invoke(this);
        }
    }
}
