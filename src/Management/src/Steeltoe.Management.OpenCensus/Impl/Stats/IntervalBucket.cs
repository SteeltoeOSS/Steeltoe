//using Steeltoe.Management.Census.Common;
//using Steeltoe.Management.Census.Tags;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Steeltoe.Management.Census.Stats
//{
//    internal sealed class IntervalBucket
//    {
//        private const long MILLIS_PER_SECOND = 1000L;
//        private const long NANOS_PER_MILLI = 1000 * 1000;

//        private static readonly IDuration ZERO = Duration.Create(0, 0);
//        private readonly ITimestamp start;
//        private readonly IDuration duration;
//        private readonly IAggregation aggregation;
//        private readonly IDictionary<IList<ITagValue>, MutableAggregation> tagValueAggregationMap = new Dictionary<IList<ITagValue>, MutableAggregation>();

//        internal IntervalBucket(ITimestamp start, IDuration duration, IAggregation aggregation)
//        {
//            if (start == null)
//            {
//                throw new ArgumentNullException(nameof(start));
//            }
//            if (duration == null)
//            {
//                throw new ArgumentNullException(nameof(duration));
//            }
//            if (!(duration.CompareTo(ZERO) > 0))
//            {
//                throw new ArgumentOutOfRangeException("Duration must be positive");
//            }
//            if (aggregation == null)
//            {
//                throw new ArgumentNullException(nameof(aggregation));
//            }

//            this.start = start;
//            this.duration = duration;
//            this.aggregation = aggregation;
//        }

//        internal IDictionary<IList<ITagValue>, MutableAggregation> TagValueAggregationMap
//        {
//            get
//            {
//                return tagValueAggregationMap;
//            }
//        }

//        internal ITimestamp Start
//        {
//            get
//            {
//                return start;
//            }
//        }

//        // Puts a new value into the internal MutableAggregations, based on the TagValues.
//        internal void Record(IList<ITagValue> tagValues, double value)
//        {
//            if (!tagValueAggregationMap.ContainsKey(tagValues))
//            {
//                tagValueAggregationMap.Add(tagValues, MutableViewData.CreateMutableAggregation(aggregation));
//            }
//            tagValueAggregationMap[tagValues].Add(value);
//        }

//        internal double GetFraction(ITimestamp now)
//        {
//            IDuration elapsedTime = now.SubtractTimestamp(start);
//            if (!(elapsedTime.CompareTo(ZERO) >= 0 && elapsedTime.CompareTo(duration) < 0))
//            {
//                throw new ArgumentOutOfRangeException("This bucket must be current.");
//            }

//            return ((double)ToMillis(elapsedTime)) / ToMillis(duration);
//        }

//        internal void ClearStats()
//        {
//            tagValueAggregationMap.Clear();
//        }
//        static long ToMillis(IDuration duration)
//        {
//            return duration.Seconds * MILLIS_PER_SECOND + duration.Nanos / NANOS_PER_MILLI;
//        }
//    }
//}
