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

////namespace Steeltoe.Management.Census.Stats
////{
////    internal sealed class IntervalBucket
////    {
////        private const long MILLIS_PER_SECOND = 1000L;
////        private const long NANOS_PER_MILLI = 1000 * 1000;

////        private static readonly IDuration ZERO = Duration.Create(0, 0);
////        private readonly ITimestamp start;
////        private readonly IDuration duration;
////        private readonly IAggregation aggregation;
////        private readonly IDictionary<IList<ITagValue>, MutableAggregation> tagValueAggregationMap = new Dictionary<IList<ITagValue>, MutableAggregation>();

////        internal IntervalBucket(ITimestamp start, IDuration duration, IAggregation aggregation)
////        {
////            if (start == null)
////            {
////                throw new ArgumentNullException(nameof(start));
////            }
////            if (duration == null)
////            {
////                throw new ArgumentNullException(nameof(duration));
////            }
////            if (!(duration.CompareTo(ZERO) > 0))
////            {
////                throw new ArgumentOutOfRangeException("Duration must be positive");
////            }
////            if (aggregation == null)
////            {
////                throw new ArgumentNullException(nameof(aggregation));
////            }

////            this.start = start;
////            this.duration = duration;
////            this.aggregation = aggregation;
////        }

////        internal IDictionary<IList<ITagValue>, MutableAggregation> TagValueAggregationMap
////        {
////            get
////            {
////                return tagValueAggregationMap;
////            }
////        }

////        internal ITimestamp Start
////        {
////            get
////            {
////                return start;
////            }
////        }

////        // Puts a new value into the internal MutableAggregations, based on the TagValues.
////        internal void Record(IList<ITagValue> tagValues, double value)
////        {
////            if (!tagValueAggregationMap.ContainsKey(tagValues))
////            {
////                tagValueAggregationMap.Add(tagValues, MutableViewData.CreateMutableAggregation(aggregation));
////            }
////            tagValueAggregationMap[tagValues].Add(value);
////        }

////        internal double GetFraction(ITimestamp now)
////        {
////            IDuration elapsedTime = now.SubtractTimestamp(start);
////            if (!(elapsedTime.CompareTo(ZERO) >= 0 && elapsedTime.CompareTo(duration) < 0))
////            {
////                throw new ArgumentOutOfRangeException("This bucket must be current.");
////            }

////            return ((double)ToMillis(elapsedTime)) / ToMillis(duration);
////        }

////        internal void ClearStats()
////        {
////            tagValueAggregationMap.Clear();
////        }
////        static long ToMillis(IDuration duration)
////        {
////            return duration.Seconds * MILLIS_PER_SECOND + duration.Nanos / NANOS_PER_MILLI;
////        }
////    }
////}
