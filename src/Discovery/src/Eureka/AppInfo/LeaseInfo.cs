// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using Steeltoe.Discovery.Eureka.Configuration;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public sealed class LeaseInfo
{
    /// <summary>
    /// Gets the client-specified time how often the client sends a heartbeat to renew the lease.
    /// </summary>
    public TimeSpan? RenewalInterval { get; private init; }

    /// <summary>
    /// Gets the client-specified time for eviction (e.g. how long to wait without renewal).
    /// </summary>
    public TimeSpan? Duration { get; private init; }

    /// <summary>
    /// Gets the registration time, in UTC, which is the time when the lease was first registered.
    /// </summary>
    public DateTime? RegistrationTimeUtc { get; private init; }

    /// <summary>
    /// Gets the time, in UTC, when the lease was last renewed.
    /// </summary>
    public DateTime? LastRenewalTimeUtc { get; private init; }

    /// <summary>
    /// Gets the de-registration time, in UTC, which is the time when the lease was removed.
    /// </summary>
    public DateTime? EvictionTimeUtc { get; private init; }

    /// <summary>
    /// Gets the time, in UTC, when the leased service was marked as UP.
    /// </summary>
    public DateTime? ServiceUpTimeUtc { get; private init; }

    private LeaseInfo()
    {
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, DebugSerializerOptions.Instance);
    }

    internal static LeaseInfo? FromJson(JsonLeaseInfo? jsonLeaseInfo)
    {
        if (jsonLeaseInfo == null)
        {
            return null;
        }

        return new LeaseInfo
        {
            RenewalInterval = NullableSecondsToTimeSpan(jsonLeaseInfo.RenewalIntervalInSeconds),
            Duration = NullableSecondsToTimeSpan(jsonLeaseInfo.DurationInSeconds),
            RegistrationTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonLeaseInfo.RegistrationTimestamp),
            LastRenewalTimeUtc =
                DateTimeConversions.FromNullableJavaMilliseconds(jsonLeaseInfo.LastRenewalTimestamp ?? jsonLeaseInfo.LastRenewalTimestampLegacy),
            EvictionTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonLeaseInfo.EvictionTimestamp),
            ServiceUpTimeUtc = DateTimeConversions.FromNullableJavaMilliseconds(jsonLeaseInfo.ServiceUpTimestamp)
        };
    }

    private static TimeSpan? NullableSecondsToTimeSpan(int? seconds)
    {
        return seconds is null ? null : TimeSpan.FromSeconds(seconds.Value);
    }

    internal static LeaseInfo FromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new LeaseInfo
        {
            RenewalInterval = options.LeaseRenewalInterval,
            Duration = options.LeaseExpirationDuration
        };
    }

    internal JsonLeaseInfo ToJson()
    {
        return new JsonLeaseInfo
        {
            RenewalIntervalInSeconds = NullableTimeSpanToSeconds(RenewalInterval),
            DurationInSeconds = NullableTimeSpanToSeconds(Duration),
            RegistrationTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(RegistrationTimeUtc),
            LastRenewalTimestampLegacy = DateTimeConversions.ToNullableJavaMilliseconds(LastRenewalTimeUtc),
            EvictionTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(EvictionTimeUtc),
            ServiceUpTimestamp = DateTimeConversions.ToNullableJavaMilliseconds(ServiceUpTimeUtc)
        };
    }

    private static int? NullableTimeSpanToSeconds(TimeSpan? timeSpan)
    {
        return timeSpan == null ? null : (int)timeSpan.Value.TotalSeconds;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not LeaseInfo other)
        {
            return false;
        }

#pragma warning disable S1067 // Expressions should not be too complex
        return RenewalInterval == other.RenewalInterval && Duration == other.Duration && RegistrationTimeUtc == other.RegistrationTimeUtc &&
            LastRenewalTimeUtc == other.LastRenewalTimeUtc && EvictionTimeUtc == other.EvictionTimeUtc && ServiceUpTimeUtc == other.ServiceUpTimeUtc;
#pragma warning restore S1067 // Expressions should not be too complex
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RenewalInterval, Duration, RegistrationTimeUtc, LastRenewalTimeUtc, EvictionTimeUtc, ServiceUpTimeUtc);
    }
}
