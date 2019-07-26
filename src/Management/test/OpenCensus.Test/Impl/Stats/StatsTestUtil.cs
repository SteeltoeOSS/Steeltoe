// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Census.Stats.Test
{
    [Obsolete]
    internal static class StatsTestUtil
    {
        private static readonly ITimestamp ZERO_TIMESTAMP = Timestamp.Create(0, 0);

        internal static IAggregationData CreateAggregationData(IAggregation aggregation, IMeasure measure, params double[] values)
        {
            MutableAggregation mutableAggregation = MutableViewData.CreateMutableAggregation(aggregation);
            foreach (double value in values)
            {
                mutableAggregation.Add(value);
            }

            return MutableViewData.CreateAggregationData(mutableAggregation, measure);
        }

        internal static IViewData CreateEmptyViewData(IView view)
        {
            return ViewData.Create(view, new Dictionary<TagValues, IAggregationData>(), ZERO_TIMESTAMP, ZERO_TIMESTAMP);
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
                    Assert.IsType<LastValueDataDouble>(actual);
                    Assert.InRange(((LastValueDataDouble)actual).LastValue, arg.LastValue - tolerance, arg.LastValue + tolerance);
                    return null;
                },
                (arg) =>
                {
                    Assert.IsType<LastValueDataLong>(actual);
                    Assert.Equal(arg.LastValue, ((LastValueDataLong)actual).LastValue);
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

            if (expected.Max == double.NegativeInfinity
                && expected.Min == double.PositiveInfinity)
            {
                Assert.True(double.IsNegativeInfinity(actual.Max));
                Assert.True(double.IsPositiveInfinity(actual.Min));
            }
            else
            {
                Assert.InRange(actual.Max, expected.Max - tolerance, expected.Max + tolerance);
                Assert.InRange(actual.Min, expected.Min - tolerance, expected.Min + tolerance);
            }

            Assert.Equal(RemoveTrailingZeros(expected.BucketCounts), RemoveTrailingZeros(actual.BucketCounts));
        }

        private static IList<long> RemoveTrailingZeros(IList<long> longs)
        {
            if (longs == null || longs.Count == 0)
            {
                return longs;
            }

            List<long> truncated = new List<long>();
            int lastIndex = longs.Count - 1;
            while (longs[lastIndex] == 0)
            {
                lastIndex--;
                if (lastIndex <= 0)
                {
                    break;
                }
            }

            for (int i = 0; i < lastIndex; i++)
            {
                truncated.Add(longs[i]);
            }

            return truncated;
        }
    }
}
