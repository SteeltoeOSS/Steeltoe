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
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Census.Stats
{
    [Obsolete("Use OpenCensus project packages")]
    internal class CumulativeMutableViewData : MutableViewData
    {
        private ITimestamp start;
        private IDictionary<TagValues, MutableAggregation> tagValueAggregationMap = new Dictionary<TagValues, MutableAggregation>();

        internal CumulativeMutableViewData(IView view, ITimestamp start)
            : base(view)
        {
            this.start = start;
        }

        internal override void Record(ITagContext context, double value, ITimestamp timestamp)
        {
            IList<ITagValue> values = GetTagValues(GetTagMap(context), View.Columns);
            var tagValues = TagValues.Create(values);
            if (!tagValueAggregationMap.ContainsKey(tagValues))
            {
                tagValueAggregationMap.Add(tagValues, CreateMutableAggregation(View.Aggregation));
            }

            tagValueAggregationMap[tagValues].Add(value);
        }

        internal override IViewData ToViewData(ITimestamp now, StatsCollectionState state)
        {
            if (state == StatsCollectionState.ENABLED)
            {
                return ViewData.Create(
                    View,
                    CreateAggregationMap(tagValueAggregationMap, View.Measure),
                    start,
                    now);
            }
            else
            {
                // If Stats state is DISABLED, return an empty ViewData.
                return ViewData.Create(
                    View,
                    new Dictionary<TagValues, IAggregationData>(),
                    ZERO_TIMESTAMP,
                    ZERO_TIMESTAMP);
            }
        }

        internal override void ClearStats()
        {
            tagValueAggregationMap.Clear();
        }

        internal override void ResumeStatsCollection(ITimestamp now)
        {
            start = now;
        }
    }
}
