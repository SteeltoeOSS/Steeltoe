using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    public class Timestamp : ITimestamp
    {
        const long MAX_SECONDS = 315576000000L;
        const int MAX_NANOS = 999999999;
        const long MILLIS_PER_SECOND = 1000L;
        const long NANOS_PER_MILLI = 1000 * 1000;
        const long NANOS_PER_SECOND = NANOS_PER_MILLI * MILLIS_PER_SECOND;
        const long TICKS_PER_NANO = 100;


        public static ITimestamp Create(long seconds, int nanos)
        {
            // TODO:
            if (seconds < -MAX_SECONDS || seconds > MAX_SECONDS)
            {
                return new Timestamp(0, 0);
            }
            if (nanos < 0 || nanos > MAX_NANOS)
            {
                return new Timestamp(0, 0);
            }
            return new Timestamp(seconds, nanos);
        }

        public static ITimestamp FromMillis(long millis)
        {
            Timestamp zero = new Timestamp(0, 0);
            long nanos = millis * NANOS_PER_MILLI;
            return zero.Plus(0, nanos);
        }

        internal Timestamp(long seconds, int nanos)
        {
            Seconds = seconds;
            Nanos = nanos;
        }

        public long Seconds { get; }
 

        public int Nanos { get; }


        public ITimestamp AddDuration(IDuration duration)
        {
            return Plus(duration.Seconds, duration.Nanos);
        }

        public ITimestamp AddNanos(long nanosToAdd)
        {
            return Plus(0, nanosToAdd);
        }


        public IDuration SubtractTimestamp(ITimestamp timestamp)
        {

            long durationSeconds = Seconds - timestamp.Seconds;
            int durationNanos = Nanos - timestamp.Nanos;
            if (durationSeconds < 0 && durationNanos > 0)
            {
                durationSeconds += 1;
                durationNanos = (int)(durationNanos - NANOS_PER_SECOND);
            }
            else if (durationSeconds > 0 && durationNanos < 0)
            {
                durationSeconds -= 1;
                durationNanos = (int)(durationNanos + NANOS_PER_SECOND);
            }
            return Duration.Create(durationSeconds, durationNanos);
        }

        public int CompareTo(ITimestamp other)
        {
            int cmp = (Seconds < other.Seconds) ? -1 : ((Seconds > other.Seconds) ? 1 : 0);
            if (cmp != 0)
            {
                return cmp;
            }
            return (Nanos < other.Nanos) ? -1 : ((Nanos > other.Nanos) ? 1 : 0);
        }

        private ITimestamp Plus(long secondsToAdd, long nanosToAdd)
        {

            if ((secondsToAdd | nanosToAdd) == 0)
            {
                return this;
            }

            long sec = Seconds + secondsToAdd;
            long nanoSeconds = Math.DivRem(nanosToAdd, NANOS_PER_SECOND, out long nanosSpill);
            sec = sec + nanoSeconds;
            long nanoAdjustment = Nanos + nanosSpill;
            return OfSecond(sec, nanoAdjustment);
        }

        private static ITimestamp OfSecond(long seconds, long nanoAdjustment)
        {
            long floor = (long)Math.Floor((double)nanoAdjustment / NANOS_PER_SECOND);
            long secs = seconds + floor;
            long nos = nanoAdjustment - floor * NANOS_PER_SECOND;
            return Create(secs, (int)nos);
        }

        public override string ToString()
        {
            return "Timestamp{"
                + "seconds=" + Seconds + ", "
                + "nanos=" + Nanos
                + "}";
        }

        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is Timestamp) {
                Timestamp that = (Timestamp)o;
                return (this.Seconds == that.Seconds)
                     && (this.Nanos == that.Nanos);
            }
            return false;
        }

        public override int GetHashCode()
        {
            long h = 1;
            h *= 1000003;
            h ^= (this.Seconds >> 32) ^ this.Seconds;
            h *= 1000003;
            h ^= this.Nanos;
            return (int)h;
        }
    }
}
