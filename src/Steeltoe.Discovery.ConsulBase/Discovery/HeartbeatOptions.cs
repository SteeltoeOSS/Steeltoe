// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Discovery.Consul.Discovery
{
    public class HeartbeatOptions
    {
        public bool Enable { get; set; } = false;

        public int TtlValue { get; set; } = 30;

        public string TtlUnit { get; set; } = "s";

        public double IntervalRatio { get; set; } = 2.0 / 3.0;

        public TimeSpan ComputeHearbeatInterval()
        {
            // heartbeat rate at ratio * ttl, but no later than ttl -1s and, (under lesser priority),
            // no sooner than 1s from now
            var interval = TtlValue * IntervalRatio;
            var max = Math.Max(interval, 1);
            var ttlMinus1 = TtlValue - 1;
            var min = Math.Min(ttlMinus1, max);
            var heartbeatInterval = (int)Math.Round(1000 * min);
            return TimeSpan.FromMilliseconds(heartbeatInterval);
        }
    }
}