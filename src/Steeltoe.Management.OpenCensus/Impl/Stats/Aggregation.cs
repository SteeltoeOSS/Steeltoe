using Steeltoe.Management.Census.Stats.Aggregations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public abstract class Aggregation : IAggregation
    {
        public abstract M Match<M>(Func<ISum, M> p0, Func<ICount, M> p1, Func<IMean, M> p2, Func<IDistribution, M> p3, Func<IAggregation, M> p4);
    }
}
