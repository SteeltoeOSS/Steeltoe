

using Steeltoe.Management.Census.Internal;
using Steeltoe.Management.Census.Trace.Export;
using System;

namespace Steeltoe.Management.Census.Trace
{
    [Obsolete("Use OpenCensus project packages")]
    internal class EventWithNanoTime<T>
    {
        private readonly long nanoTime;
        private readonly T @event;

        public EventWithNanoTime(long nanoTime, T @event) {
            this.nanoTime = nanoTime;
            this.@event = @event;
        }

        internal ITimedEvent<T> ToSpanDataTimedEvent(ITimestampConverter timestampConverter)
        {
            return TimedEvent<T>.Create(timestampConverter.ConvertNanoTime(nanoTime), @event);
        }
    }
}
