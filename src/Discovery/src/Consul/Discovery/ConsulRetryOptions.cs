// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// Configuration values used for the retry feature.
/// </summary>
public class ConsulRetryOptions
{
    internal const int DefaultMaxRetryAttempts = 6;
    internal const int DefaultInitialRetryInterval = 1000;
    internal const double DefaultRetryMultiplier = 1.1;
    internal const int DefaultMaxRetryInterval = 2000;

    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled, defaults false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the initial interval to use during retries, defaults 1000ms.
    /// </summary>
    public int InitialInterval { get; set; } = DefaultInitialRetryInterval;

    /// <summary>
    /// Gets or sets the maximum interval to use during retries, defaults 2000ms.
    /// </summary>
    public int MaxInterval { get; set; } = DefaultMaxRetryInterval;

    /// <summary>
    /// Gets or sets the multiplier used when doing retries, default 1.1.
    /// </summary>
    public double Multiplier { get; set; } = DefaultRetryMultiplier;

    /// <summary>
    /// Gets or sets the maximum number of retries, default 6.
    /// </summary>
    public int MaxAttempts { get; set; } = DefaultMaxRetryAttempts;
}
