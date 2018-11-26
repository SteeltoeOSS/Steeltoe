using Steeltoe.Management.Census.Common;
using System;

namespace Steeltoe.Management.Census.Internal
{
    [Obsolete("Use OpenCensus project packages")]
    internal class TimestampConverter : ITimestampConverter
    {
        private readonly ITimestamp timestamp;
        private readonly long nanoTime;

        // Returns a WallTimeConverter initialized to now.
        public static ITimestampConverter Now(IClock clock)
        {
            return new TimestampConverter(clock.Now, clock.NowNanos);
        }

  
        public ITimestamp ConvertNanoTime(long nanoTime)
        {
            return timestamp.AddNanos(nanoTime - this.nanoTime);
        }

        private TimestampConverter(ITimestamp timestamp, long nanoTime)
        {
            this.timestamp = timestamp;
            this.nanoTime = nanoTime;
        }
    }
}
