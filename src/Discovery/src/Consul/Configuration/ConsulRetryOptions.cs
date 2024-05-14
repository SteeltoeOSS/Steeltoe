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
    /// Gets or sets a value indicating whether to try again When registering the running app fails. Default value: false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the time to wait (in milliseconds) after the first registration failure. Default value: 1000.
    /// </summary>
    public int InitialInterval { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the upper bound (in milliseconds) for intervals. Default value: 2000.
    /// </summary>
    public int MaxInterval { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the factor to increment the next interval with. Default value: 1.1.
    /// </summary>
    public double Multiplier { get; set; } = 1.1;

    /// <summary>
    /// Gets or sets the maximum number of registration attempts (if retries are enabled). Default value: 6.
    /// </summary>
    public int MaxAttempts { get; set; } = 6;
}
