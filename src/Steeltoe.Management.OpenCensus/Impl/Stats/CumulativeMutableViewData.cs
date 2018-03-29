using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Tags;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
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
                    start, now);
            }
            else
            {
                // If Stats state is DISABLED, return an empty ViewData.
                return ViewData.Create(
                    View,
                    new Dictionary<TagValues, IAggregationData>(),
                    ZERO_TIMESTAMP, ZERO_TIMESTAMP);
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
