using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class LastValueDataDouble : AggregationData, ILastValueDataDouble
    {
        LastValueDataDouble() { }

        public double LastValue { get; }

        LastValueDataDouble(double lastValue)
        {
            this.LastValue = lastValue;
        }

        public static ILastValueDataDouble Create(double lastValue)
        {
            return new LastValueDataDouble(lastValue);
        }

        public override string ToString()
        {
            return "LastValueDataDouble{"
                + "lastValue=" + LastValue
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is LastValueDataDouble)
            {
                LastValueDataDouble that = (LastValueDataDouble)o;
                return (DoubleUtil.ToInt64(this.LastValue) == DoubleUtil.ToInt64(that.LastValue));
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.LastValue) >> 32) ^ DoubleUtil.ToInt64(this.LastValue);
            return (int)h;
        }

        public override M Match<M>(
            Func<ISumDataDouble, M> p0,
            Func<ISumDataLong, M> p1,
            Func<ICountData, M> p2,
            Func<IMeanData, M> p3,
            Func<IDistributionData, M> p4,
            Func<ILastValueDataDouble, M> p5,
            Func<ILastValueDataLong, M> p6,
            Func<IAggregationData, M> defaultFunction)
        {
            return p5.Invoke(this);
        }
    }
}
