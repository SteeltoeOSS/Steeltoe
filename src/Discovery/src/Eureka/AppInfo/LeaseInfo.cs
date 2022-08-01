// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;

namespace Steeltoe.Discovery.Eureka.AppInfo;

public class LeaseInfo
{
    public const int DefaultRenewalIntervalInSecs = 30;
    public const int DefaultDurationInSecs = 90;

    public int RenewalIntervalInSecs { get; internal set; }

    public int DurationInSecs { get; internal set; }

    public long RegistrationTimestamp { get; internal set; }

    public long LastRenewalTimestamp { get; internal set; }

    public long LastRenewalTimestampLegacy { get; internal set; }

    public long EvictionTimestamp { get; internal set; }

    public long ServiceUpTimestamp { get; internal set; }

    internal static LeaseInfo FromJson(JsonLeaseInfo leaseInfo)
    {
        var info = new LeaseInfo();
        if (leaseInfo != null)
        {
            info.RenewalIntervalInSecs = leaseInfo.RenewalIntervalInSecs;
            info.DurationInSecs = leaseInfo.DurationInSecs;
            info.RegistrationTimestamp = DateTimeConversions.FromJavaMillis(leaseInfo.RegistrationTimestamp).Ticks;
            info.LastRenewalTimestamp = DateTimeConversions.FromJavaMillis(leaseInfo.LastRenewalTimestamp).Ticks;
            info.LastRenewalTimestampLegacy = DateTimeConversions.FromJavaMillis(leaseInfo.LastRenewalTimestampLegacy).Ticks;
            info.EvictionTimestamp = DateTimeConversions.FromJavaMillis(leaseInfo.EvictionTimestamp).Ticks;
            info.ServiceUpTimestamp = DateTimeConversions.FromJavaMillis(leaseInfo.ServiceUpTimestamp).Ticks;
        }

        return info;
    }

    internal static LeaseInfo FromConfig(IEurekaInstanceConfig config)
    {
        var info = new LeaseInfo
        {
            RenewalIntervalInSecs = config.LeaseRenewalIntervalInSeconds,
            DurationInSecs = config.LeaseExpirationDurationInSeconds
        };
        return info;
    }

    internal LeaseInfo()
    {
        RenewalIntervalInSecs = DefaultRenewalIntervalInSecs;
        DurationInSecs = DefaultDurationInSecs;
    }

    internal JsonLeaseInfo ToJson()
    {
        var leaseInfo = new JsonLeaseInfo
        {
            RenewalIntervalInSecs = RenewalIntervalInSecs,
            DurationInSecs = DurationInSecs,
            RegistrationTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(RegistrationTimestamp, DateTimeKind.Utc)),
            LastRenewalTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestamp, DateTimeKind.Utc)),
            LastRenewalTimestampLegacy = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestampLegacy, DateTimeKind.Utc)),
            EvictionTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(EvictionTimestamp, DateTimeKind.Utc)),
            ServiceUpTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(ServiceUpTimestamp, DateTimeKind.Utc))
        };
        return leaseInfo;
    }
}
