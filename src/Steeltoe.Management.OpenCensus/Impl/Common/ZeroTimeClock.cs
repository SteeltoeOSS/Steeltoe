using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Census.Common
{
    [Obsolete("Use OpenCensus project packages")]
    internal sealed class ZeroTimeClock : IClock
    {
        public static readonly ZeroTimeClock INSTANCE = new ZeroTimeClock();
        private static readonly ITimestamp ZERO_TIMESTAMP = Timestamp.Create(0, 0);

        public ITimestamp Now
        {
            get
            {
                return ZERO_TIMESTAMP;
            }
        }

        public long NowNanos
        {
            get
            {
                return 0;
            }
        }
    }
}
