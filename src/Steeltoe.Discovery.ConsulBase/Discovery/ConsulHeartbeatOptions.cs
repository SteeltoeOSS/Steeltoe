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
    /// <summary>
    /// Configuration values used for the heartbeat checks
    /// </summary>
    public class ConsulHeartbeatOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether heartbeats are enabled, defaults true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the time to live heartbeat time, defaults 30
        /// </summary>
        public int TtlValue { get; set; } = 30;

        /// <summary>
        /// Gets or sets the time unit of the TtlValue, defaults = "s"
        /// </summary>
        public string TtlUnit { get; set; } = "s";

        /// <summary>
        /// Gets or sets the interval ratio
        /// </summary>
        public double IntervalRatio { get; set; } = 2.0 / 3.0;

        /// <summary>
        /// Gets the time to live setting
        /// </summary>
        public string Ttl
        {
            get
            {
                return TtlValue.ToString() + TtlUnit;
            }
        }

        internal TimeSpan ComputeHearbeatInterval()
        {
            // heartbeat rate at ratio * ttl, but no later than ttl -1s and, (under lesser priority),
            // no sooner than 1s from now
            double interval = TtlValue * IntervalRatio;
            double max = Math.Max(interval, 1);
            int ttlMinus1 = TtlValue - 1;
            double min = Math.Min(ttlMinus1, max);
            double heartbeatInterval = (int)Math.Round(1000 * min);
            return TimeSpan.FromMilliseconds(heartbeatInterval);
        }
    }
}