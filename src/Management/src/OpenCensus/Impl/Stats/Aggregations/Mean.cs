using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class Mean : Aggregation , IMean
    {
        internal Mean() { }

        private static readonly Mean INSTANCE = new Mean();


        public static IMean Create()
        {
            return INSTANCE;
        }

        public override M Match<M>(Func<ISum, M> p0, Func<ICount, M> p1, Func<IMean, M> p2, Func<IDistribution, M> p3, Func<ILastValue, M> p4, Func<IAggregation, M> p5)
        {
            return p2.Invoke(this);
        }

        public override String ToString()
        {
            return "Mean{"
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Mean)
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            return h;
        }

    }
}
