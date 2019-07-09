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

namespace Steeltoe.Management.Census.Common
{
    [Obsolete("Use OpenCensus project packages")]
    internal class DateTimeOffsetClock : IClock
    {
        internal const long MILLIS_PER_SECOND = 1000L;
        internal const long NANOS_PER_MILLI = 1000 * 1000;
        internal const long NANOS_PER_SECOND = NANOS_PER_MILLI * MILLIS_PER_SECOND;

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
                return new Timestamp(nowSecTicks, (int)excessNanos);
            }
        }

        public long NowNanos
        {
            get
            {
                var millis = DateTimeOffset.UtcNow.UtcTicks / TimeSpan.TicksPerMillisecond;
                return millis * NANOS_PER_MILLI;
            }
        }
    }
}
