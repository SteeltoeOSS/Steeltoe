using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class Count : Aggregation, ICount
    {
        Count() { }

        private static readonly Count INSTANCE = new Count();

        public static ICount Create()
        {
            return INSTANCE;
        }
        
        public override M Match<M>(Func<ISum, M> p0, Func<ICount, M> p1, Func<IMean, M> p2, Func<IDistribution, M> p3, Func<IAggregation, M> p4)
        {
            return p1.Invoke(this);
        }

        public override string ToString()
        {
            return "Count{"
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Count)
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
