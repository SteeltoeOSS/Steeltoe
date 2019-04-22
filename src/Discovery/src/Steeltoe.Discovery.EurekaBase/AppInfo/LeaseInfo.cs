// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            LeaseInfo info = new LeaseInfo();
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
            LeaseInfo info = new LeaseInfo()
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
            JsonLeaseInfo jinfo = new JsonLeaseInfo()
            {
                RenewalIntervalInSecs = this.RenewalIntervalInSecs,
                DurationInSecs = this.DurationInSecs,
                RegistrationTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(this.RegistrationTimestamp, DateTimeKind.Utc)),
                LastRenewalTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(this.LastRenewalTimestamp, DateTimeKind.Utc)),
                LastRenewalTimestampLegacy = DateTimeConversions.ToJavaMillis(new DateTime(this.LastRenewalTimestampLegacy, DateTimeKind.Utc)),
                EvictionTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(this.EvictionTimestamp, DateTimeKind.Utc)),
                ServiceUpTimestamp = DateTimeConversions.ToJavaMillis(new DateTime(this.ServiceUpTimestamp, DateTimeKind.Utc))
            };
            return jinfo;
        }
    }
}
