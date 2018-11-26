using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class BucketBoundaries : IBucketBoundaries
    {
        public IList<double> Boundaries { get; }

        BucketBoundaries(IList<double> boundaries)
        {

            this.Boundaries = boundaries;
        }
        public static IBucketBoundaries Create(IList<double> bucketBoundaries)
        {
            if (bucketBoundaries == null)
            {
                throw new ArgumentNullException(nameof(bucketBoundaries));
            }
            List<Double> bucketBoundariesCopy = new List<double>(bucketBoundaries);

            if (bucketBoundariesCopy.Count > 1)
            {
                double lower = bucketBoundariesCopy[0];
                for (int i = 1; i < bucketBoundariesCopy.Count; i++)
                {
                    double next = bucketBoundariesCopy[i];
                    if (!(lower < next))
                    {
                        throw new ArgumentOutOfRangeException("Bucket boundaries not sorted.");
                    }

                    lower = next;
                }
            }
            return new BucketBoundaries(bucketBoundariesCopy.AsReadOnly());
        }

        public override string ToString()
        {
            return "BucketBoundaries{"
                + "boundaries=" + Collections.ToString(Boundaries)
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is BucketBoundaries)
            {
                BucketBoundaries that = (BucketBoundaries)o;
                return (this.Boundaries.SequenceEqual(that.Boundaries));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Boundaries.GetHashCode();
            return h;
        }
    }
}
