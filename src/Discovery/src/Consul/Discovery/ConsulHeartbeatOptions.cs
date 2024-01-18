// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// Configuration values used for the heartbeat checks.
/// </summary>
public sealed class ConsulHeartbeatOptions
{
    /// <summary>
    /// Gets the time-to-live setting.
    /// </summary>
    internal string TimeToLive => TtlValue + TtlUnit;

    /// <summary>
    /// Gets or sets a value indicating whether heartbeats are enabled, default true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the time to live heartbeat time, default 30.
    /// </summary>
    public int TtlValue { get; set; } = 30;

    /// <summary>
    /// Gets or sets the time unit of the TtlValue, default "s".
    /// </summary>
    public string? TtlUnit { get; set; } = "s";

    /// <summary>
    /// Gets or sets the interval ratio.
    /// </summary>
    public double IntervalRatio { get; set; } = 2.0 / 3.0;

    internal TimeSpan ComputeHeartbeatInterval()
    {
        TimeSpan oneSecond = TimeSpan.FromSeconds(1);
        var ttl = DateTimeConversions.ToTimeSpan(TtlValue, TtlUnit);

        // heartbeat rate at ratio * ttl, but no later than ttl -1s and, (under lesser priority),
        // no sooner than 1s from now
        TimeSpan interval = TimeSpan.FromMilliseconds(ttl.TotalMilliseconds * IntervalRatio);
        TimeSpan max = interval > oneSecond ? interval : oneSecond;
        TimeSpan ttlMinusOneSecond = ttl - oneSecond;
        TimeSpan min = ttlMinusOneSecond < max ? ttlMinusOneSecond : max;
        return min;
    }
}
