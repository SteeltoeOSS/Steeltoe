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
    internal const int DefaultRenewalIntervalInSecs = 30;
    internal const int DefaultDurationInSecs = 90;

    public int RenewalIntervalInSecs { get; private set; }
    public int DurationInSecs { get; private set; }
    public long RegistrationTimestamp { get; private set; }
    public long LastRenewalTimestamp { get; private set; }
    public long LastRenewalTimestampLegacy { get; private set; }
    public long EvictionTimestamp { get; private set; }
    public long ServiceUpTimestamp { get; private set; }

    public LeaseInfo()
        : this(DefaultRenewalIntervalInSecs, DefaultDurationInSecs)
    {
    }

    public LeaseInfo(int renewalIntervalInSecs, int durationInSecs)
    {
        RenewalIntervalInSecs = renewalIntervalInSecs;
        DurationInSecs = durationInSecs;
    }

    internal static LeaseInfo FromJson(JsonLeaseInfo? jsonLeaseInfo)
    {
        var leaseInfo = new LeaseInfo();

        if (jsonLeaseInfo != null)
        {
            leaseInfo.RenewalIntervalInSecs = jsonLeaseInfo.RenewalIntervalInSecs;
            leaseInfo.DurationInSecs = jsonLeaseInfo.DurationInSecs;
            leaseInfo.RegistrationTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.RegistrationTimestamp).Ticks;
            leaseInfo.LastRenewalTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.LastRenewalTimestamp).Ticks;
            leaseInfo.LastRenewalTimestampLegacy = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.LastRenewalTimestampLegacy).Ticks;
            leaseInfo.EvictionTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.EvictionTimestamp).Ticks;
            leaseInfo.ServiceUpTimestamp = DateTimeConversions.FromJavaMillis(jsonLeaseInfo.ServiceUpTimestamp).Ticks;
        }

        return leaseInfo;
    }

    internal static LeaseInfo FromConfiguration(EurekaInstanceOptions options)
    {
        ArgumentGuard.NotNull(options);

        return new LeaseInfo(options.LeaseRenewalIntervalInSeconds, options.LeaseExpirationDurationInSeconds);
    }

    internal JsonLeaseInfo ToJson()
    {
        return new JsonLeaseInfo
        {
            RenewalIntervalInSecs = RenewalIntervalInSecs,
            DurationInSecs = DurationInSecs,
            RegistrationTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(RegistrationTimestamp, DateTimeKind.Utc)),
            LastRenewalTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestamp, DateTimeKind.Utc)),
            LastRenewalTimestampLegacy = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestampLegacy, DateTimeKind.Utc)),
            EvictionTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(EvictionTimestamp, DateTimeKind.Utc)),
            ServiceUpTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(ServiceUpTimestamp, DateTimeKind.Utc))
        };
    }
}
