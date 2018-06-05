using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public class CountData : AggregationData, ICountData
    {
        public long Count { get; }

        internal CountData(long count)
        {
            this.Count = count;
        }

        public static ICountData Create(long count)
        {
            return new CountData(count);
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
            return p2.Invoke(this);
        }
        public override string ToString()
        {
            return "CountData{"
                + "count=" + Count
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is CountData)
            {
                CountData that = (CountData)o;
                return (this.Count == that.Count);
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (this.Count >> 32) ^ this.Count;
            return (int)h;
        }

    }
}
