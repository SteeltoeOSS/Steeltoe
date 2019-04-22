using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TimedEvent<T> : ITimedEvent<T>
    {
        public static ITimedEvent<T> Create(ITimestamp timestamp, T @event) {
            return new TimedEvent<T>(timestamp, @event);
        }

        public ITimestamp Timestamp { get; }
        public T Event { get; }

        internal TimedEvent(ITimestamp timestamp, T @event)
        {

            this.Timestamp = timestamp;
            this.Event = @event;
        }

        public override string ToString()
        {
            return "TimedEvent{"
                + "timestamp=" + Timestamp + ", "
                + "event=" + Event
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }

            if (o is TimedEvent<T>)
            {
                TimedEvent<T> that = (TimedEvent<T>)o;
                return (this.Timestamp.Equals(that.Timestamp))
                     && (this.Event.Equals(that.Event));
            }
            return false;
        }

        public override int GetHashCode()
        {
            int h = 1;
            h *= 1000003;
            h ^= this.Timestamp.GetHashCode();
            h *= 1000003;
            h ^= this.Event.GetHashCode();
            return h;
        }
    }
}
