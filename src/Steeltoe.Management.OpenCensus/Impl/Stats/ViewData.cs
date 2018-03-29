using Steeltoe.Management.Census.Common;
using Steeltoe.Management.Census.Stats.Aggregations;
using Steeltoe.Management.Census.Stats.Measures;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Steeltoe.Management.Census.Stats
{
    public sealed class ViewData : IViewData
    {
        public IView View { get; }
        public IDictionary<TagValues, IAggregationData> AggregationMap { get; }
        public ITimestamp Start { get; }
        public ITimestamp End { get; }

        internal ViewData(IView view, IDictionary<TagValues, IAggregationData> aggregationMap, ITimestamp start, ITimestamp end)
        {
            if (view == null)
            {
                throw new ArgumentNullException(nameof(view));
            }
            this.View = view;
            if (aggregationMap == null)
            {
                throw new ArgumentNullException(nameof(aggregationMap));
            }
            this.AggregationMap = aggregationMap;

            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }
            this.Start = start;
            if (end == null)
            {
                throw new ArgumentNullException(nameof(end));
            }
            this.End = end;
        }

        public static IViewData Create(IView view, IDictionary<TagValues, IAggregationData> map, ITimestamp start, ITimestamp end)
        {
            IDictionary<TagValues, IAggregationData> deepCopy = new Dictionary<TagValues, IAggregationData>();
            foreach (var entry in map)
            {
                CheckAggregation(view.Aggregation, entry.Value, view.Measure);
                deepCopy.Add(entry.Key, entry.Value);
            }
            return new ViewData(
                view,
                new ReadOnlyDictionary<TagValues, IAggregationData>(deepCopy),
                start,
                end);
        }

        public override String ToString()
        {
            return "ViewData{"
                + "view=" + View + ", "
                + "aggregationMap=" + Collections.ToString(AggregationMap) + ", "
                + "start=" + Start + ", "
                + "end=" + End
                + "}";
        }

        public override bool Equals(Object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is ViewData)
            {
                ViewData that = (ViewData)o;
                return (this.View.Equals(that.View))
                     && (this.AggregationMap.SequenceEqual(that.AggregationMap))
                     && (this.Start.Equals(that.Start))
                     && (this.End.Equals(that.End));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.View.GetHashCode();
            h *= 1000003;
            h ^= this.AggregationMap.GetHashCode();
            h *= 1000003;
            h ^= this.Start.GetHashCode();
            h *= 1000003;
            h ^= this.End.GetHashCode();
            return h;
        }
        private static void CheckAggregation(IAggregation aggregation, IAggregationData aggregationData, IMeasure measure)
        {
            aggregation.Match<object>(
                (arg) =>
                {
                    measure.Match<object>(
                        (arg1) =>
                        {
                            if (!(aggregationData is ISumDataDouble))
                            {
                                throw new ArgumentException(CreateErrorMessageForAggregation(aggregation, aggregationData));
                            }
                            return null;
                        },
                        (arg1) =>
                        {
                            if (!(aggregationData is ISumDataLong))
                            {
                                throw new ArgumentException(CreateErrorMessageForAggregation(aggregation, aggregationData));
                            }
                            return null;
                        },
                        (arg1) =>
                        {
                            throw new ArgumentException();
                        });
                    return null;
                }, 
                (arg) =>
                {
                    if (!(aggregationData is ICountData))
                    {
                        throw new ArgumentException(CreateErrorMessageForAggregation(aggregation, aggregationData));
                    }
                    return null;
                },
                (arg) =>
                {
                    if (!(aggregationData is IMeanData))
                    {
                        throw new ArgumentException(CreateErrorMessageForAggregation(aggregation, aggregationData));
                    }
                    return null;
                },
                (arg) =>
                {
                    if (!(aggregationData is IDistributionData))
                    {
                        throw new ArgumentException(CreateErrorMessageForAggregation(aggregation, aggregationData));
                    }
                    return null;
                }, 
                (arg) =>
                {
                    throw new ArgumentException();
                }
                );
        }

        private static String CreateErrorMessageForAggregation(IAggregation aggregation, IAggregationData aggregationData)
        {
            return "Aggregation and AggregationData types mismatch. "
                + "Aggregation: "
                + aggregation
                + " AggregationData: "
                + aggregationData;
        }

    }
}
