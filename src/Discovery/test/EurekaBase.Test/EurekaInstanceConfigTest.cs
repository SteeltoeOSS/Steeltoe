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

using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaInstanceConfigTest : AbstractBaseTest
    {
        [Fact]
        public void DefaultConstructor_InitializedWithDefaults()
        {
            EurekaInstanceConfig config = new EurekaInstanceConfig();

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
