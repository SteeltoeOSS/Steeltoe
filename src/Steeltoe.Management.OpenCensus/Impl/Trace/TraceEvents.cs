using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace
{
    internal class TraceEvents<T>
    {
        private int totalRecordedEvents = 0;
        private readonly EvictingQueue<T> events;

        public EvictingQueue<T> Events
        {
            get
            {
                return events;
            }
        }
        public int NumberOfDroppedEvents
        {
            get { return totalRecordedEvents - events.Count; }
        }

        public TraceEvents(int maxNumEvents)
        {
            events = new EvictingQueue<T>(maxNumEvents);
        }

        internal void AddEvent(T @event) {
            totalRecordedEvents++;
            events.Add(@event);
        }
    }
}
