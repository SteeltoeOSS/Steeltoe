using Steeltoe.Management.Census.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    internal class DateTimeOffsetClock : IClock
    {
        const long MILLIS_PER_SECOND = 1000L;
        const long NANOS_PER_MILLI = 1000 * 1000;
        const long NANOS_PER_SECOND = NANOS_PER_MILLI * MILLIS_PER_SECOND;

        public static readonly DateTimeOffsetClock INSTANCE = new DateTimeOffsetClock();
        public static IClock GetInstance()
        {
            return INSTANCE;
        }

        public ITimestamp Now
        {
            get
            {
                var nowNanoTicks = NowNanos;
                var nowSecTicks = nowNanoTicks / NANOS_PER_SECOND;
                var excessNanos = nowNanoTicks - (nowSecTicks * NANOS_PER_SECOND);
                return new Timestamp(nowSecTicks, (int) excessNanos);

            }
        }

        public long NowNanos => DateTimeOffset.UtcNow.UtcTicks * 100;


    }
}
