using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Stats.Aggregations
{
    public class DistributionData : AggregationData, IDistributionData
    {
        public double Mean { get; }
        public long Count { get; }
        public double Min { get; }
        public double Max { get; }
        public double SumOfSquaredDeviations { get; }
        public IList<long> BucketCounts { get; }

        internal DistributionData(double mean, long count, double min, double max, double sumOfSquaredDeviations, IList<long> bucketCounts)
        {
            this.Mean = mean;
            this.Count = count;
            this.Min = min;
            this.Max = max;
            this.SumOfSquaredDeviations = sumOfSquaredDeviations;
            if (bucketCounts == null)
            {
                throw new ArgumentNullException(nameof(bucketCounts));
            }
            this.BucketCounts = bucketCounts;
        }
        public static IDistributionData Create(double mean,long count, double min, double max, double sumOfSquaredDeviations, IList<long> bucketCounts)
        {
            if (!double.IsPositiveInfinity(min) ||  !double.IsNegativeInfinity(max))
            {
                if (!(min <= max))
                {
                    throw new ArgumentOutOfRangeException("max should be greater or equal to min.");
                }
            }
            if (bucketCounts == null)
            {
                throw new ArgumentNullException(nameof(bucketCounts));
            }
            IList<long> bucketCountsCopy = new List<long>(bucketCounts).AsReadOnly();

            return new DistributionData(
                mean, count, min, max, sumOfSquaredDeviations, bucketCountsCopy);
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
            return p4.Invoke(this);
        }

        public override String ToString()
        {
            return "DistributionData{"
                + "mean=" + Mean + ", "
                + "count=" + Count + ", "
                + "min=" + Min + ", "
                + "max=" + Max + ", "
                + "sumOfSquaredDeviations=" + SumOfSquaredDeviations + ", "
                + "bucketCounts=" + Collections.ToString(BucketCounts)
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is DistributionData)
            {
                DistributionData that = (DistributionData)o;
                return (DoubleUtil.ToInt64(this.Mean) == DoubleUtil.ToInt64(that.Mean))
                     && (this.Count == that.Count)
                     && (DoubleUtil.ToInt64(this.Min) == DoubleUtil.ToInt64(that.Min))
                     && (DoubleUtil.ToInt64(this.Max) == DoubleUtil.ToInt64(that.Max))
                     && (DoubleUtil.ToInt64(this.SumOfSquaredDeviations) == DoubleUtil.ToInt64(that.SumOfSquaredDeviations))
                     && (this.BucketCounts.SequenceEqual(that.BucketCounts));
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.Mean) >> 32) ^ DoubleUtil.ToInt64(this.Mean);
            h *= 1000003;
            h ^= (this.Count >> 32) ^ this.Count;
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.Min) >> 32) ^ DoubleUtil.ToInt64(this.Min);
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.Max) >> 32) ^ DoubleUtil.ToInt64(this.Max);
            h *= 1000003;
            h ^= (DoubleUtil.ToInt64(this.SumOfSquaredDeviations) >> 32) ^ DoubleUtil.ToInt64(this.SumOfSquaredDeviations);
            h *= 1000003;
            h ^= this.BucketCounts.GetHashCode();
            return (int)h;
        }
    }
}
