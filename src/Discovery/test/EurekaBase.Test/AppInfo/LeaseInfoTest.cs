// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Discovery.Eureka.Test;
using Steeltoe.Discovery.Eureka.Transport;
using Steeltoe.Discovery.Eureka.Util;
using System;
using Xunit;

namespace Steeltoe.Discovery.Eureka.AppInfo.Test
{
    public class LeaseInfoTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_Defaults()
        {
            var info = new LeaseInfo();
            Assert.Equal(LeaseInfo.Default_DurationInSecs, info.DurationInSecs);
            Assert.Equal(LeaseInfo.Default_RenewalIntervalInSecs, info.RenewalIntervalInSecs);
        }

        [Fact]
        public void FromJson_Correct()
        {
            var jinfo = new JsonLeaseInfo()
            {
                RenewalIntervalInSecs = 100,
                DurationInSecs = 200,
                RegistrationTimestamp = 1457973741708,
                LastRenewalTimestamp = 1457973741708,
                LastRenewalTimestampLegacy = 1457973741708,
                EvictionTimestamp = 1457973741708,
                ServiceUpTimestamp = 1457973741708
            };

            var result = LeaseInfo.FromJson(jinfo);
            Assert.Equal(100, result.RenewalIntervalInSecs);
            Assert.Equal(200, result.DurationInSecs);
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.RegistrationTimestamp, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestamp, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestampLegacy, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.EvictionTimestamp, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.ServiceUpTimestamp, DateTimeKind.Utc)));
        }

        [Fact]
        public void ToJson_Correct()
        {
            var jinfo = new JsonLeaseInfo()
            {
                RenewalIntervalInSecs = 100,
                DurationInSecs = 200,
                RegistrationTimestamp = 1457973741708,
                LastRenewalTimestamp = 1457973741708,
                LastRenewalTimestampLegacy = 1457973741708,
                EvictionTimestamp = 1457973741708,
                ServiceUpTimestamp = 1457973741708
            };

            var result = LeaseInfo.FromJson(jinfo);

            jinfo = result.ToJson();

            Assert.Equal(100, result.RenewalIntervalInSecs);
            Assert.Equal(200, result.DurationInSecs);
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.RegistrationTimestamp, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestamp, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.LastRenewalTimestampLegacy, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.EvictionTimestamp, DateTimeKind.Utc)));
            Assert.Equal(1457973741708, DateTimeConversions.ToJavaMillis(new DateTime(result.ServiceUpTimestamp, DateTimeKind.Utc)));
        }

        [Fact]
        public void FromConfig_Correct()
        {
            var config = new EurekaInstanceConfig();
            var info = LeaseInfo.FromConfig(config);
            Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.RenewalIntervalInSecs);
            Assert.Equal(config.LeaseExpirationDurationInSeconds, info.DurationInSecs);
        }
    }
}
