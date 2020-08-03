// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaInstanceConfigTest : AbstractBaseTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            var config = new EurekaInstanceConfig();

            var thisHostName = config.GetHostName(false);
            var thisHostAddress = config.GetHostAddress(false);

            Assert.False(config.IsInstanceEnabledOnInit);
            Assert.Equal(EurekaInstanceConfig.Default_NonSecurePort, config.NonSecurePort);
            Assert.Equal(EurekaInstanceConfig.Default_SecurePort, config.SecurePort);
            Assert.True(config.IsNonSecurePortEnabled);
            Assert.False(config.SecurePortEnabled);
            Assert.Equal(EurekaInstanceConfig.Default_LeaseRenewalIntervalInSeconds, config.LeaseRenewalIntervalInSeconds);
            Assert.Equal(EurekaInstanceConfig.Default_LeaseExpirationDurationInSeconds, config.LeaseExpirationDurationInSeconds);
            Assert.Equal(thisHostName + ":" + config.SecurePort, config.SecureVirtualHostName);
            Assert.Equal(thisHostAddress, config.IpAddress);
            Assert.Equal(EurekaInstanceConfig.Default_Appname, config.AppName);
            Assert.Equal(EurekaInstanceConfig.Default_StatusPageUrlPath, config.StatusPageUrlPath);
            Assert.Equal(EurekaInstanceConfig.Default_HomePageUrlPath, config.HomePageUrlPath);
            Assert.Equal(EurekaInstanceConfig.Default_HealthCheckUrlPath, config.HealthCheckUrlPath);
            Assert.NotNull(config.MetadataMap);
            Assert.Empty(config.MetadataMap);
            Assert.Equal(DataCenterName.MyOwn, config.DataCenterInfo.Name);
        }
    }
}
