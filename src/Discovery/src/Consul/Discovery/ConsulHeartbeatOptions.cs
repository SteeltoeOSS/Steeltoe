// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// Configuration values used for the heartbeat checks.
/// </summary>
public class ConsulHeartbeatOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether heartbeats are enabled, defaults true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the time to live heartbeat time, defaults 30.
    /// </summary>
    public int TtlValue { get; set; } = 30;

    /// <summary>
    /// Gets or sets the time unit of the TtlValue, defaults = "s".
    /// </summary>
    public string TtlUnit { get; set; } = "s";

    /// <summary>
    /// Gets or sets the interval ratio.
    /// </summary>
    public double IntervalRatio { get; set; } = 2.0 / 3.0;

    /// <summary>
    /// Gets the time to live setting.
    /// </summary>
    public string Ttl
    {
        get
        {
            return TtlValue + TtlUnit;
        }
    }

    internal TimeSpan ComputeHeartbeatInterval()
    {
        var second = TimeSpan.FromSeconds(1);
        var ttl = DateTimeConversions.ToTimeSpan(TtlValue, TtlUnit);

        // heartbeat rate at ratio * ttl, but no later than ttl -1s and, (under lesser priority),
        // no sooner than 1s from now
        var interval = TimeSpan.FromMilliseconds(ttl.TotalMilliseconds * IntervalRatio);
        var max = interval > second ? interval : second;
        var ttlMinus1Sec = ttl - second;
        var min = ttlMinus1Sec < max ? ttlMinus1Sec : max;
        return min;
    }
}
