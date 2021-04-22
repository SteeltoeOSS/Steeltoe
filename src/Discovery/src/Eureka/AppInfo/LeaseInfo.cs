// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Discovery.Eureka.AppInfo
{
    public class LeaseInfo
    {
        public const int Default_RenewalIntervalInSecs = 30;
        public const int Default_DurationInSecs = 90;

        public int RenewalIntervalInSecs { get; set; }

        public int DurationInSecs { get; set; }

        public long RegistrationTimestamp { get; set; }

        public long LastRenewalTimestamp { get; set; }

        public long LastRenewalTimestampLegacy { get; set; }

        public long EvictionTimestamp { get; set; }

        public long ServiceUpTimestamp { get; set; }

        public LeaseInfo()
        {
            RenewalIntervalInSecs = Default_RenewalIntervalInSecs;
            DurationInSecs = Default_DurationInSecs;
        }

        internal static LeaseInfo FromConfig(IEurekaInstanceConfig config)
        {
            var info = new LeaseInfo()
            {
                RenewalIntervalInSecs = config.LeaseRenewalIntervalInSeconds,
                DurationInSecs = config.LeaseExpirationDurationInSeconds
            };
            return info;
        }
    }
}
