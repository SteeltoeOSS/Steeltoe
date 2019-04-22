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

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// Configuration values used for the retry feature
    /// </summary>
    public class ConsulRetryOptions
    {
        internal const int DEFAULT_MAX_RETRY_ATTEMPTS = 6;
        internal const int DEFAULT_INITIAL_RETRY_INTERVAL = 1000;
        internal const double DEFAULT_RETRY_MULTIPLIER = 1.1;
        internal const int DEFAULT_MAX_RETRY_INTERVAL = 2000;

        /// <summary>
        /// Gets or sets a value indicating whether retries are enabled, defaults false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the initial interval to use during retrys, defaults 1000ms
        /// </summary>
        public int InitialInterval { get; set; } = DEFAULT_INITIAL_RETRY_INTERVAL;

        /// <summary>
        /// Gets or sets the maximum interval to use during retrys, defaults 2000ms
        /// </summary>
        public int MaxInterval { get; set; } = DEFAULT_MAX_RETRY_INTERVAL;

        /// <summary>
        /// Gets or sets the multiplier used when doing retrys, default 1.1
        /// </summary>
        public double Multiplier { get; set; } = DEFAULT_RETRY_MULTIPLIER;

        /// <summary>
        /// Gets or sets the maximum number of retrys, default 6
        /// </summary>
        public int MaxAttempts { get; set; } = DEFAULT_MAX_RETRY_ATTEMPTS;
    }
}
