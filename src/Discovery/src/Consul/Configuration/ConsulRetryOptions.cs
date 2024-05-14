// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Consul.Configuration;

/// <summary>
/// Configuration values used for the retry feature.
/// </summary>
public sealed class ConsulRetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether retries are enabled, default false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the initial interval to use during retries, default 1000ms.
    /// </summary>
    public int InitialInterval { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the maximum interval to use during retries, default 2000ms.
    /// </summary>
    public int MaxInterval { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the multiplier used when doing retries, default 1.1.
    /// </summary>
    public double Multiplier { get; set; } = 1.1;

    /// <summary>
    /// Gets or sets the maximum number of attempts, default 6.
    /// </summary>
    public int MaxAttempts { get; set; } = 6;
}
