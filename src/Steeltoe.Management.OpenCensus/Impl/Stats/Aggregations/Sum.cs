using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class Sum : Aggregation ,ISum
    {
        internal Sum() { }

        private static readonly Sum INSTANCE = new Sum();

        public static ISum Create()
        {
            return INSTANCE;
        }
        public override M Match<M>(Func<ISum, M> p0, Func<ICount, M> p1, Func<IMean, M> p2, Func<IDistribution, M> p3, Func<ILastValue, M> p4, Func<IAggregation, M> p5)
        {
            return p0.Invoke(this);
        }

        public override string ToString()
        {
            return "Sum{"
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Sum) {
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
