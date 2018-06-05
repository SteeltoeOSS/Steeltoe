using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public sealed class Distribution : Aggregation, IDistribution
    {
        Distribution() { }

        public IBucketBoundaries BucketBoundaries { get; }

        Distribution(IBucketBoundaries bucketBoundaries)
        {
            if (bucketBoundaries == null)
            {
                throw new ArgumentNullException("Null bucketBoundaries");
            }
            BucketBoundaries = bucketBoundaries;
        }

        public static IDistribution Create(IBucketBoundaries bucketBoundaries)
        {
            if (bucketBoundaries == null)
            {
                throw new ArgumentNullException(nameof(bucketBoundaries));
            }
            return new Distribution(bucketBoundaries);
        }

        public override M Match<M>(Func<ISum, M> p0, Func<ICount, M> p1, Func<IMean, M> p2, Func<IDistribution, M> p3, Func<ILastValue, M> p4, Func<IAggregation, M> p5)
        {
            return p3.Invoke(this);
        }

        public override string ToString()
        {
            return "Distribution{"
                + "bucketBoundaries=" + BucketBoundaries
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Distribution) {
                Distribution that = (Distribution)o;
                return (this.BucketBoundaries.Equals(that.BucketBoundaries));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.BucketBoundaries.GetHashCode();
            return h;
        }

    }
}
