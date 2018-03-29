using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class SumDataDouble : AggregationData, ISumDataDouble
    {
        public double Sum { get; }

        internal SumDataDouble(double sum)
        {
            this.Sum = sum;
        }

        public static ISumDataDouble Create(double sum)
        {
            return new SumDataDouble(sum);
        }


        public override M Match<M>(
            Func<ISumDataDouble, M> p0,
            Func<ISumDataLong, M> p1,
            Func<ICountData, M> p2,
            Func<IMeanData, M> p3,
            Func<IDistributionData, M> p4,
            Func<IAggregationData, M> defaultFunction)
        {
            return p0.Invoke(this);
        }

        public override string ToString()
        {
            return "SumDataDouble{"
                + "sum=" + Sum
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is SumDataDouble) {
                SumDataDouble that = (SumDataDouble)o;
                return (DoubleUtil.ToInt64(this.Sum) == DoubleUtil.ToInt64(that.Sum));
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.Sum) >> 32) ^ DoubleUtil.ToInt64(this.Sum);
            return (int)h;
        }

    }
}
