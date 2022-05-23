// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using System;

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class LeaseInfo
    {
        public const int Default_RenewalIntervalInSecs = 30;
        public const int Default_DurationInSecs = 90;

        public int RenewalIntervalInSecs { get; internal set; }

        public int DurationInSecs { get; internal set; }

        public long RegistrationTimestamp { get; internal set; }

        public long LastRenewalTimestamp { get; internal set; }

        public long LastRenewalTimestampLegacy { get; internal set; }

        public long EvictionTimestamp { get; internal set; }

        public long ServiceUpTimestamp { get; internal set; }

        internal static LeaseInfo FromJson(JsonLeaseInfo jinfo)
        {
            var info = new LeaseInfo();
            if (jinfo != null)
            {
                info.RenewalIntervalInSecs = jinfo.RenewalIntervalInSecs;
                info.DurationInSecs = jinfo.DurationInSecs;
                info.RegistrationTimestamp = DateTimeConversions.FromJavaMillis(jinfo.RegistrationTimestamp).Ticks;
                info.LastRenewalTimestamp = DateTimeConversions.FromJavaMillis(jinfo.LastRenewalTimestamp).Ticks;
                info.LastRenewalTimestampLegacy = DateTimeConversions.FromJavaMillis(jinfo.LastRenewalTimestampLegacy).Ticks;
                info.EvictionTimestamp = DateTimeConversions.FromJavaMillis(jinfo.EvictionTimestamp).Ticks;
                info.ServiceUpTimestamp = DateTimeConversions.FromJavaMillis(jinfo.ServiceUpTimestamp).Ticks;
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
            RenewalIntervalInSecs = Default_RenewalIntervalInSecs;
            DurationInSecs = Default_DurationInSecs;
        }

        internal JsonLeaseInfo ToJson()
        {
            var jinfo = new JsonLeaseInfo
            {
                RenewalIntervalInSecs = RenewalIntervalInSecs,
                DurationInSecs = DurationInSecs,
                RegistrationTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(RegistrationTimestamp, DateTimeKind.Utc)),
                LastRenewalTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestamp, DateTimeKind.Utc)),
                LastRenewalTimestampLegacy = DateTimeConversions.ToJavaMillis(new DateTime(LastRenewalTimestampLegacy, DateTimeKind.Utc)),
                EvictionTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(EvictionTimestamp, DateTimeKind.Utc)),
                ServiceUpTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(ServiceUpTimestamp, DateTimeKind.Utc))
            };
            return jinfo;
        }
    }
}
