using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    internal abstract class MutableViewData
    {
        private const long MILLIS_PER_SECOND = 1000L;
        private const long NANOS_PER_MILLI = 1000 * 1000;

        internal static readonly ITagValue UNKNOWN_TAG_VALUE = null;

        internal static readonly ITimestamp ZERO_TIMESTAMP = Timestamp.Create(0, 0);

        internal IView View { get; }

        protected MutableViewData(IView view)
        {
            this.View = view;
        }

   
        internal static MutableViewData Create(IView view, ITimestamp start)
        {
            return new CumulativeMutableViewData(view, start);
        }



        /** Record double stats with the given tags. */
        internal abstract void Record(ITagContext context, double value, ITimestamp timestamp);

        /** Record long stats with the given tags. */
        internal void Record(ITagContext tags, long value, ITimestamp timestamp)
        {
            // TODO(songya): shall we check for precision loss here?
            Record(tags, (double)value, timestamp);
        }

        /** Convert this {@link MutableViewData} to {@link ViewData}. */
        internal abstract IViewData ToViewData(ITimestamp now, StatsCollectionState state);

        // Clear recorded stats.
        internal abstract void ClearStats();

        // Resume stats collection, and reset Start Timestamp (for CumulativeMutableViewData), or refresh
        // bucket list (for InternalMutableViewData).
        internal abstract void ResumeStatsCollection(ITimestamp now);

        internal static IDictionary<ITagKey, ITagValue> GetTagMap(ITagContext ctx)
        {
            if (ctx is TagContext)
             {
                return ((TagContext)ctx).Tags;
            } else {
                IDictionary<ITagKey, ITagValue> tags = new Dictionary<ITagKey, ITagValue>();
                foreach (var tag in ctx)
                {
                    tags.Add(tag.Key, tag.Value);
                }
                return tags;
            }
        }

        internal static IList<ITagValue> GetTagValues( IDictionary<ITagKey,ITagValue> tags, IList<ITagKey> columns)
        {
            IList<ITagValue> tagValues = new List<ITagValue>(columns.Count);
            // Record all the measures in a "Greedy" way.
            // Every view aggregates every measure. This is similar to doing a GROUPBY view’s keys.
            for (int i = 0; i < columns.Count; ++i)
            {
                ITagKey tagKey = columns[i];
                if (!tags.ContainsKey(tagKey))
                {
                    // replace not found key values by null.
                    tagValues.Add(UNKNOWN_TAG_VALUE);
                }
                else
                {
                    tagValues.Add(tags[tagKey]);
                }
            }
            return tagValues;
        }

        // Returns the milliseconds representation of a Duration.
        internal static long ToMillis(IDuration duration)
        {
            return duration.Seconds * MILLIS_PER_SECOND + duration.Nanos / NANOS_PER_MILLI;
        }

 
        internal static MutableAggregation CreateMutableAggregation(IAggregation aggregation)
        {
            return aggregation.Match(
                CreateMutableSum,
                CreateMutableCount,
                CreateMutableMean,
                CreateMutableDistribution,
                ThrowArgumentException);
        }

 
        internal static IAggregationData CreateAggregationData(MutableAggregation aggregation, IMeasure measure)
        {
            return aggregation.Match<IAggregationData>(
                (msum) => 
                {
                    return measure.Match<IAggregationData>(
                        (mdouble) =>
                        {
                            return SumDataDouble.Create(msum.Sum);
                        },
                        (mlong) =>
                        {

                            return SumDataLong.Create((long)Math.Round(msum.Sum));
                        },
                        (invalid) =>
                        {
                            throw new ArgumentException();
                        });
                 },
                CreateCountData,
                CreateMeanData,
                CreateDistributionData);
        }

        // Covert a mapping from TagValues to MutableAggregation, to a mapping from TagValues to
        // AggregationData.
        internal static IDictionary<T, IAggregationData> CreateAggregationMap<T>(IDictionary<T, MutableAggregation> tagValueAggregationMap, IMeasure measure)
        {
            IDictionary<T, IAggregationData> map = new Dictionary<T, IAggregationData>();
            foreach (var entry in tagValueAggregationMap)
            {
                map.Add(entry.Key, CreateAggregationData(entry.Value, measure));
            }
            return map;
        }

        private static Func<ISum, MutableAggregation> CreateMutableSum { get; } = (s) => { return MutableSum.Create(); };
        private static Func<ICount, MutableAggregation> CreateMutableCount { get; } = (s) => { return MutableCount.Create(); };
        private static Func<IMean, MutableAggregation> CreateMutableMean { get; } = (s) => { return MutableMean.Create(); };
        private static Func<IDistribution, MutableAggregation> CreateMutableDistribution { get; } = (s) => { return MutableDistribution.Create(s.BucketBoundaries); };
        private static Func<IAggregation, MutableAggregation> ThrowArgumentException { get; } = (s) => { throw new ArgumentException(); };
        private static Func<MutableCount, IAggregationData> CreateCountData { get; } = (s) => { return CountData.Create(s.Count); };
        private static Func<MutableMean, IAggregationData> CreateMeanData { get; } = (s) => { return MeanData.Create(s.Mean, s.Count); };
        private static Func<MutableDistribution, IAggregationData> CreateDistributionData { get; } = (s) => 
            {
                List<long> boxedBucketCounts = new List<long>();
                foreach (long bucketCount in s.BucketCounts)
                {
                    boxedBucketCounts.Add(bucketCount);
                }
                return DistributionData.Create(
                    s.Mean,
                    s.Count,
                    s.Min,
                    s.Max,
                    s.SumOfSquaredDeviations,
                    boxedBucketCounts);
            };
    }
}
