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
