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
using System;

namespace Steeltoe.Management.Census.Trace.Export
{
    [Obsolete("Use OpenCensus project packages")]
    public sealed class TimedEvent<T> : ITimedEvent<T>
    {
        public static ITimedEvent<T> Create(ITimestamp timestamp, T @event)
        {
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
                return this.Timestamp.Equals(that.Timestamp)
                     && this.Event.Equals(that.Event);
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
