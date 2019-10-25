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

using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    public static class StatsExtensions
    {
        public static bool ContainsKeys(this IView view, List<ITagKey> keys)
        {
            var columns = view.Columns;
            foreach (var key in keys)
            {
                if (!columns.Contains(key))
                {
                    return false;
                }
            }

            return true;
        }

        public static IAggregationData SumWithTags(this IViewData viewData, IList<ITagValue> values = null)
        {
            return viewData.AggregationMap.WithTags(values).Sum(viewData.View);
        }

        public static IDictionary<TagValues, IAggregationData> WithTags(this IDictionary<TagValues, IAggregationData> aggMap, IList<ITagValue> values)
        {
            Dictionary<TagValues, IAggregationData> results = new Dictionary<TagValues, IAggregationData>();

            foreach (var kvp in aggMap)
            {
                if (TagValuesMatch(kvp.Key.Values, values))
                {
                    results.Add(kvp.Key, kvp.Value);
                }
            }

            return results;
        }

        public static IAggregationData Sum(this IDictionary<TagValues, IAggregationData> aggMap, IView view)
        {
            var sum = MutableViewData.CreateMutableAggregation(view.Aggregation);
            foreach (IAggregationData agData in aggMap.Values)
            {
                Sum(sum, agData);
            }

            return MutableViewData.CreateAggregationData(sum, view.Measure);
        }

        private static bool TagValuesMatch(IList<ITagValue> aggValues, IList<ITagValue> values)
        {
            if (values == null)
            {
                return true;
            }

            if (aggValues.Count != values.Count)
            {
                return false;
            }

            for (int i = 0; i < aggValues.Count; i++)
            {
                var v1 = aggValues[i];
                var v2 = values[i];

                // Null matches any aggValue
                if (v2 == null)
                {
                    continue;
                }

                if (!v2.Equals(v1))
                {
                    return false;
                }
            }

            return true;
        }

        private static void Sum(MutableAggregation combined, IAggregationData data)
        {
            data.Match<object>(
                (arg) =>
                {
                    MutableSum sum = combined as MutableSum;
                    if (sum != null)
                    {
                        sum.Add(arg.Sum);
                    }

                    return null;
                },
                (arg) =>
                {
                    MutableSum sum = combined as MutableSum;
                    if (sum != null)
                    {
                        sum.Add(arg.Sum);
                    }

                    return null;
                },
                (arg) =>
                {
                    MutableCount count = combined as MutableCount;
                    if (count != null)
                    {
                        count.Add(arg.Count);
                    }

                    return null;
                },
                (arg) =>
                {
                    MutableMean mean = combined as MutableMean;
                    if (mean != null)
                    {
                        mean.Count = mean.Count + arg.Count;
                        mean.Sum = mean.Sum + (arg.Count * arg.Mean);
                        if (arg.Min < mean.Min)
                        {
                            mean.Min = arg.Min;
                        }

                        if (arg.Max > mean.Max)
                        {
                            mean.Max = arg.Max;
                        }
                    }

                    return null;
                },
                (arg) =>
                {
                    MutableDistribution dist = combined as MutableDistribution;
                    if (dist != null)
                    {
                        // Algorithm for calculating the combination of sum of squared deviations:
                        // https://en.wikipedia.org/wiki/Algorithms_for_calculating_variance#Parallel_algorithm.
                        if (dist.Count + arg.Count > 0)
                        {
                            double delta = arg.Mean - dist.Mean;
                            dist.SumOfSquaredDeviations =
                                dist.SumOfSquaredDeviations
                                    + arg.SumOfSquaredDeviations
                                    + (Math.Pow(delta, 2)
                                        * dist.Count
                                        * arg.Count
                                        / (dist.Count + arg.Count));
                        }

                        dist.Count += arg.Count;
                        dist.Sum += arg.Mean * arg.Count;
                        dist.Mean = dist.Sum / dist.Count;

                        if (arg.Min < dist.Min)
                        {
                            dist.Min = arg.Min;
                        }

                        if (arg.Max > dist.Max)
                        {
                            dist.Max = arg.Max;
                        }

                        IList<long> bucketCounts = arg.BucketCounts;
                        for (int i = 0; i < bucketCounts.Count; i++)
                        {
                            dist.BucketCounts[i] += bucketCounts[i];
                        }
                    }

                    return null;
                },
                (arg) =>
                {
                    MutableLastValue lastValue = combined as MutableLastValue;
                    if (lastValue != null)
                    {
                        lastValue.Initialized = true;
                        if (double.IsNaN(lastValue.LastValue))
                        {
                            lastValue.LastValue = arg.LastValue;
                        }
                        else
                        {
                            lastValue.LastValue += arg.LastValue;
                        }
                    }

                    return null;
                },
                (arg) =>
                {
                    MutableLastValue lastValue = combined as MutableLastValue;
                    if (lastValue != null)
                    {
                        lastValue.Initialized = true;
                        if (double.IsNaN(lastValue.LastValue))
                        {
                            lastValue.LastValue = arg.LastValue;
                        }
                        else
                        {
                            lastValue.LastValue += arg.LastValue;
                        }
                    }

                    return null;
                },
                (arg) =>
                {
                    throw new ArgumentException();
                });
        }
    }
}
