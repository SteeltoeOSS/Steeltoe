// <copyright file="DateTimeOffsetClock.cs" company="OpenCensus Authors">
// Copyright 2018, OpenCensus Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

namespace OpenCensus.Common
{
    using System;

    internal class DateTimeOffsetClock : IClock
    {
        public static readonly DateTimeOffsetClock Instance = new DateTimeOffsetClock();

        internal const long MillisPerSecond = 1000L;
        internal const long NanosPerMilli = 1000 * 1000;
        internal const long NanosPerSecond = NanosPerMilli * MillisPerSecond;

        public ITimestamp Now
        {
            get
            {
                var nowNanoTicks = this.NowNanos;
                var nowSecTicks = nowNanoTicks / NanosPerSecond;
                var excessNanos = nowNanoTicks - (nowSecTicks * NanosPerSecond);
                return Timestamp.Create(nowSecTicks, (int)excessNanos);
            }
        }

        public long NowNanos
        {
            get
            {
                var millis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return millis * NanosPerMilli;
            }
        }

        public static IClock GetInstance()
        {
            return Instance;
        }
    }
}
