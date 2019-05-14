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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TimedEvents<T> : ITimedEvents<T>
    {
        public static ITimedEvents<T> Create(IList<ITimedEvent<T>> events, int droppedEventsCount)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            List<ITimedEvent<T>> ev = new List<ITimedEvent<T>>();
            ev.AddRange(events);
            return new TimedEvents<T>(ev.AsReadOnly(), droppedEventsCount);
        }

        public IList<ITimedEvent<T>> Events { get; }

        public int DroppedEventsCount { get; }

        internal TimedEvents(IList<ITimedEvent<T>> events, int droppedEventsCount)
        {
            if (events == null)
            {
                throw new ArgumentNullException("Null events");
            }

            this.Events = events;
            this.DroppedEventsCount = droppedEventsCount;
        }

        public override string ToString()
        {
            return "TimedEvents{"
                + "events=" + Events + ", "
                + "droppedEventsCount=" + DroppedEventsCount
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TimedEvents<T>)
            {
                TimedEvents<T> that = (TimedEvents<T>)o;
                return this.Events.SequenceEqual(that.Events)
                     && (this.DroppedEventsCount == that.DroppedEventsCount);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Events.GetHashCode();
            h *= 1000003;
            h ^= this.DroppedEventsCount;
            return h;
        }
    }
}
