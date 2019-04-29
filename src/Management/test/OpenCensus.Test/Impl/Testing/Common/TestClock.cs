using Steeltoe.Management.Census.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Census.Testing.Common
{ 
    public class TestClock : IClock
    {
        private const int NUM_NANOS_PER_SECOND = 1000 * 1000 * 1000;
        private ITimestamp currentTime = Timestamp.Create(1493419949, 223123456);
        private object _lck = new object();
        public static TestClock Create()
        {
            return new TestClock();
        }

        public static TestClock Create(ITimestamp time)
        {
            TestClock clock = new TestClock();
            clock.Time = time;
            return clock;
        }

        public ITimestamp Time
        {
            get
            {
                lock (_lck) { return currentTime; }

            }
            set
            {
                lock (_lck) { currentTime = value; }
            }

        }
        public void AdvanceTime(IDuration duration)
        {
            lock (_lck) { currentTime = currentTime.AddDuration(duration); }
        }

        public ITimestamp Now
        {
            get
            {
                lock (_lck) { return currentTime; }
            }
        }

        public long NowNanos
        {
            get
            {
                lock (_lck) { return GetNanos(currentTime); }
            }
        }
        private static long GetNanos(ITimestamp time)
        {
            var nanoSeconds = time.Seconds * NUM_NANOS_PER_SECOND;
            return nanoSeconds + time.Nanos;
        }
        private TestClock() { }
    }
}
