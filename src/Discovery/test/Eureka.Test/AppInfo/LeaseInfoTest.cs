// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.Test;
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
        public void FromConfig_Correct()
        {
            var config = new EurekaInstanceConfig();
            var info = LeaseInfo.FromConfig(config);
            Assert.Equal(config.LeaseRenewalIntervalInSeconds, info.RenewalIntervalInSecs);
            Assert.Equal(config.LeaseExpirationDurationInSeconds, info.DurationInSecs);
        }
    }
}
