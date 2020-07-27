// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;
using OpenCensus.Tags;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Metrics
{
    internal static class MetricsHelpers
    {
        internal static IAggregationData SumWithTags(IViewData viewData, IList<ITagValue> tagValues = null)
        {
            var withTags = viewData.AggregationMap.WithTags(tagValues);
            return StatsExtensions.Sum(withTags, viewData.View);
        }

        private static IDictionary<TagValues, IAggregationData> WithTags(this IDictionary<TagValues, IAggregationData> aggMap, IList<ITagValue> values)
        {
            var results = new Dictionary<TagValues, IAggregationData>();

            foreach (var kvp in aggMap)
            {
                if (TagValuesMatch(kvp.Key.Values, values))
                {
                    results.Add(kvp.Key, kvp.Value);
                }
            }

            return results;
        }

        private static bool TagValuesMatch(IEnumerable<ITagValue> aggValues, IEnumerable<ITagValue> values)
        {
            if (values == null)
            {
                return true;
            }

            if (aggValues.Count() != values.Count())
            {
                return false;
            }

            var first = aggValues.GetEnumerator();
            var second = values.GetEnumerator();

            while (first.MoveNext())
            {
                second.MoveNext();

                // Null matches any aggValue
                if (second.Current == null)
                {
                    continue;
                }

                if (!second.Current.Equals(first.Current))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
