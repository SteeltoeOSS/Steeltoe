// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
