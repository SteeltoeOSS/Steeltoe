using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal abstract class MutableAggregation
    {

        protected MutableAggregation() { }

        // Tolerance for double comparison.
        private const double TOLERANCE = 1e-6;

        internal abstract void Add(double value);

        internal abstract void Combine(MutableAggregation other, double fraction);

        internal abstract T Match<T>( Func<MutableSum, T> p0, Func<MutableCount, T> p1, Func<MutableMean, T> p2, Func<MutableDistribution, T> p3, Func<MutableLastValue, T> p4);
    }
}
