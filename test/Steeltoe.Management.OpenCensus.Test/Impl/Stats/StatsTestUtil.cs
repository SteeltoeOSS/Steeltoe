using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    internal static class StatsTestUtil
    {
        internal static IAggregationData CreateAggregationData(IAggregation aggregation, IMeasure measure, params double[] values)
        {
            MutableAggregation mutableAggregation = MutableViewData.CreateMutableAggregation(aggregation);
            foreach (double value in values)
            {
                mutableAggregation.Add(value);
            }
            return MutableViewData.CreateAggregationData(mutableAggregation, measure);
        }
        internal static void AssertAggregationMapEquals(
            IDictionary<TagValues, IAggregationData> actual,
            IDictionary<TagValues, IAggregationData> expected,
            double tolerance)
        {
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(expected.Keys, actual.Keys);

            foreach (var tagValues in actual.Keys)
            {
                var act = actual[tagValues];
                var exp = expected[tagValues];
                AssertAggregationDataEquals(exp, act, tolerance);
            }
        }
        internal static void AssertAggregationDataEquals(
            IAggregationData expected,
            IAggregationData actual,
            double tolerance)
        {
            expected.Match<object>(
                (arg) =>
                {
                    Assert.IsType<SumDataDouble>(actual);
                    Assert.InRange(((SumDataDouble)actual).Sum, arg.Sum - tolerance, arg.Sum + tolerance);
                    return null;
                },
                (arg) =>

                {
                    Assert.IsType<SumDataLong>(actual);
                    Assert.InRange(((SumDataLong)actual).Sum, arg.Sum - tolerance, arg.Sum + tolerance);
                    return null;
                },
                (arg) =>

                {
                    Assert.IsType<CountData>(actual);
                    Assert.Equal(arg.Count, ((CountData)actual).Count);
                    return null;
                },
                (arg) =>
                {
                    Assert.IsType<MeanData>(actual);
                    Assert.InRange(((MeanData)actual).Mean, arg.Mean - tolerance, arg.Mean + tolerance);
                    return null;
                },
                (arg) =>
                {
                    Assert.IsType<DistributionData>(actual);
                    AssertDistributionDataEquals(arg, (IDistributionData)actual, tolerance);
                    return null;
                },
                (arg) =>
                 {
                     throw new ArgumentException();
                 });
        }
        private static void AssertDistributionDataEquals(
            IDistributionData expected,
            IDistributionData actual,
            double tolerance)
        {
            
            Assert.InRange(actual.Mean, expected.Mean - tolerance, expected.Mean + tolerance);
            Assert.Equal(expected.Count, actual.Count);
            Assert.InRange(actual.SumOfSquaredDeviations, expected.SumOfSquaredDeviations - tolerance, expected.SumOfSquaredDeviations + tolerance);

            if (expected.Max == Double.NegativeInfinity
                && expected.Min == Double.PositiveInfinity)
            {
                Assert.True(Double.IsNegativeInfinity(actual.Max));
                Assert.True(Double.IsPositiveInfinity(actual.Min));
            }
            else
            {
                Assert.InRange(actual.Max, expected.Max - tolerance, expected.Max + tolerance);
                Assert.InRange(actual.Min, expected.Min - tolerance, expected.Min + tolerance);
            }

            Assert.Equal(RemoveTrailingZeros(expected.BucketCounts), RemoveTrailingZeros((actual).BucketCounts));
        }
        private static IList<long> RemoveTrailingZeros(IList<long> longs)
        {
            if (longs == null || longs.Count == 0)
            {
                return longs;
            }
   
            List<long> truncated = new List<long>();
            int lastIndex = longs.Count - 1;
            while(longs[lastIndex] == 0)
            {
                lastIndex--;
                if (lastIndex <= 0)
                {
                    break;
                }
            }
            for (int i = 0; i < lastIndex; i++) truncated.Add(longs[i]);

            return truncated;
       
        }
    }
}
