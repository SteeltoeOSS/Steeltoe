// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class LeaseInfo
{
    /// <summary>
    /// Gets the client-specified time in seconds how often renewal takes place.
    /// </summary>
    public int RenewalIntervalInSecs { get; private set; }

    /// <summary>
    /// Gets the client-specified time in seconds for eviction (e.g. how long to wait without renewal).
    /// </summary>
    public int DurationInSecs { get; private set; }

    /// <summary>
    /// Gets the registration time (the time when the lease was first registered).
    /// </summary>
    public long RegistrationTimestamp { get; private set; }

    /// <summary>
    /// Gets the time when the lease was last renewed.
    /// </summary>
    public long LastRenewalTimestamp { get; private set; }

    /// <summary>
    /// Gets the de-registration time (the time when the lease was removed).
    /// </summary>
    public long EvictionTimestamp { get; private set; }

    /// <summary>
    /// Gets the time when the leased service was marked as UP.
    /// </summary>
    public long ServiceUpTimestamp { get; private set; }

    private LeaseInfo()
    {
    }

    internal static LeaseInfo FromJson(JsonLeaseInfo? jsonLeaseInfo)
    {
        if (jsonLeaseInfo == null)
        {
            return new LeaseInfo();
        }

        return new LeaseInfo
        {
            RenewalIntervalInSecs = jsonLeaseInfo.RenewalIntervalInSecs,
            DurationInSecs = jsonLeaseInfo.DurationInSecs,
            RegistrationTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.RegistrationTimestamp).Ticks,
            LastRenewalTimestamp = jsonLeaseInfo.LastRenewalTimestamp == null
                ? DateTimeConversions.FromJavaMillis(jsonLeaseInfo.LastRenewalTimestampLegacy).Ticks
                : DateTimeConversions.FromJavaMillis(jsonLeaseInfo.LastRenewalTimestamp.Value).Ticks,
            EvictionTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.EvictionTimestamp).Ticks,
            ServiceUpTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.ServiceUpTimestamp).Ticks
        };
    }

    internal static LeaseInfo FromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentGuard.NotNull(options);

        return new LeaseInfo
        {
            RenewalIntervalInSecs = options.LeaseRenewalIntervalInSeconds,
            DurationInSecs = options.LeaseExpirationDurationInSeconds
        };
    }

    internal JsonLeaseInfo ToJson()
    {
        return new JsonLeaseInfo
        {
            RenewalIntervalInSecs = RenewalIntervalInSecs,
            DurationInSecs = DurationInSecs,
            RegistrationTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(RegistrationTimestamp, DateTimeKind.Utc)),
            LastRenewalTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestamp, DateTimeKind.Utc)),
            EvictionTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(EvictionTimestamp, DateTimeKind.Utc)),
            ServiceUpTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(ServiceUpTimestamp, DateTimeKind.Utc))
        };
    }
}
