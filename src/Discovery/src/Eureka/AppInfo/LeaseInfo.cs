// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class LeaseInfo
{
    private static readonly DateTime DefaultDateTimeUtc = DateTime.SpecifyKind(default, DateTimeKind.Utc);

    /// <summary>
    /// Gets the client-specified time how often renewal takes place.
    /// </summary>
    public TimeSpan RenewalInterval { get; private init; }

    /// <summary>
    /// Gets the client-specified time for eviction (e.g. how long to wait without renewal).
    /// </summary>
    public TimeSpan Duration { get; private init; }

    /// <summary>
    /// Gets the registration time, in UTC, which is the time when the lease was first registered.
    /// </summary>
    public DateTime RegistrationTimeUtc { get; private init; } = DefaultDateTimeUtc;

    /// <summary>
    /// Gets the time, in UTC, when the lease was last renewed.
    /// </summary>
    public DateTime LastRenewalTimeUtc { get; private init; } = DefaultDateTimeUtc;

    /// <summary>
    /// Gets the de-registration time, in UTC, which is the time when the lease was removed.
    /// </summary>
    public DateTime EvictionTimeUtc { get; private init; } = DefaultDateTimeUtc;

    /// <summary>
    /// Gets the time, in UTC, when the leased service was marked as UP.
    /// </summary>
    public DateTime ServiceUpTimeUtc { get; private init; } = DefaultDateTimeUtc;

    private LeaseInfo()
    {
    }

    internal static LeaseInfo? FromJson(JsonLeaseInfo? jsonLeaseInfo)
    {
        if (jsonLeaseInfo == null)
        {
            return null;
        }

        return new LeaseInfo
        {
            RenewalInterval = TimeSpan.FromSeconds(jsonLeaseInfo.RenewalIntervalInSeconds),
            Duration = TimeSpan.FromSeconds(jsonLeaseInfo.DurationInSeconds),
            RegistrationTimeUtc = DateTimeConversions.FromJavaMilliseconds(jsonLeaseInfo.RegistrationTimestamp),
            LastRenewalTimeUtc = jsonLeaseInfo.LastRenewalTimestamp == null
                ? DateTimeConversions.FromJavaMilliseconds(jsonLeaseInfo.LastRenewalTimestampLegacy)
                : DateTimeConversions.FromJavaMilliseconds(jsonLeaseInfo.LastRenewalTimestamp.Value),
            EvictionTimeUtc = DateTimeConversions.FromJavaMilliseconds(jsonLeaseInfo.EvictionTimestamp),
            ServiceUpTimeUtc = DateTimeConversions.FromJavaMilliseconds(jsonLeaseInfo.ServiceUpTimestamp)
        };
    }

    internal static LeaseInfo FromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentGuard.NotNull(options);

        return new LeaseInfo
        {
            RenewalInterval = TimeSpan.FromSeconds(options.LeaseRenewalIntervalInSeconds),
            Duration = TimeSpan.FromSeconds(options.LeaseExpirationDurationInSeconds)
        };
    }

    internal JsonLeaseInfo ToJson()
    {
        return new JsonLeaseInfo
        {
            RenewalIntervalInSeconds = (int)RenewalInterval.TotalSeconds,
            DurationInSeconds = (int)Duration.TotalSeconds,
            RegistrationTimestamp = DateTimeConversions.ToJavaMilliseconds(RegistrationTimeUtc),
            LastRenewalTimestamp = DateTimeConversions.ToJavaMilliseconds(LastRenewalTimeUtc),
            EvictionTimestamp = DateTimeConversions.ToJavaMilliseconds(EvictionTimeUtc),
            ServiceUpTimestamp = DateTimeConversions.ToJavaMilliseconds(ServiceUpTimeUtc)
        };
    }
}
