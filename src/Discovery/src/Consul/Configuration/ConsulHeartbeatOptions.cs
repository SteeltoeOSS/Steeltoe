// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Configuration;

/// <summary>
/// Configuration values used for the heartbeat checks.
/// </summary>
public sealed class ConsulHeartbeatOptions
{
    internal string TimeToLive => $"{TtlValue}{TtlUnit}";

    /// <summary>
    /// Gets or sets a value indicating whether the running app periodically sends TTL (time-to-live) heartbeats. Default value: true.
    /// </summary>
    /// <remarks>
    /// This setting only has effect when <see cref="ConsulDiscoveryOptions.RegisterHealthCheck" /> is true.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how often a TTL heartbeat must be sent to be considered healthy. Default value: 30.
    /// </summary>
    public int TtlValue { get; set; } = 30;

    /// <summary>
    /// Gets or sets the unit for <see cref="TtlValue" /> ("ms", "s", "m" or "h"). Default value: s.
    /// </summary>
    public string? TtlUnit { get; set; } = "s";

    /// <summary>
    /// Gets or sets the rate at which the running app sends TTL heartbeats, relative to <see cref="TtlValue" /> and <see cref="TtlUnit" />. Default value:
    /// 0.66.
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
