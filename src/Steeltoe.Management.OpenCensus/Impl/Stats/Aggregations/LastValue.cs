using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class LastValue : Aggregation, ILastValue
    {
        LastValue() { }

        private static readonly LastValue INSTANCE = new LastValue();

        public static ILastValue Create()
        {
            return INSTANCE;
        }

        public override M Match<M>(Func<ISum, M> p0, Func<ICount, M> p1, Func<IMean, M> p2, Func<IDistribution, M> p3, Func<ILastValue, M> p4, Func<IAggregation, M> p5)
        {
            return p4.Invoke(this);
        }

        public override string ToString()
        {
            return "LastValue{"
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is LastValue)
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
