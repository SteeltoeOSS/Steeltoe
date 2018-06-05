using System;
using System.Collections.Generic;
using System.Text;


namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class LastValueDataLong : AggregationData, ILastValueDataLong
    {
        LastValueDataLong() { }

        public long LastValue { get; }

        LastValueDataLong(long lastValue)
        {
            this.LastValue = lastValue;
        }

        public static ILastValueDataLong Create(long lastValue)
        {
            return new LastValueDataLong(lastValue);
        }

        public override string ToString()
        {
            return "LastValueDataLong{"
                + "lastValue=" + LastValue
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is LastValueDataLong)
            {
                LastValueDataLong that = (LastValueDataLong)o;
                return (this.LastValue == that.LastValue);
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (this.LastValue >> 32) ^ this.LastValue;
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
            return p6.Invoke(this);
        }
    }
}
