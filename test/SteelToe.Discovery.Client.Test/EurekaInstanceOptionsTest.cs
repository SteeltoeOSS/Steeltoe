//
// Copyright 2015 the original author or authors.
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
//

using SteelToe.Discovery.Eureka.AppInfo;
using Xunit;

namespace SteelToe.Discovery.Client.Test
{
    public class EurekaInstanceOptionsTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_Intializes_Defaults()
        {
            EurekaInstanceOptions opts = new EurekaInstanceOptions();
            Assert.Null(opts.InstanceId);
            Assert.Null(opts.AppName);
            Assert.Null(opts.AppGroupName);
            Assert.True(opts.IsInstanceEnabledOnInit);
            Assert.Equal(EurekaInstanceOptions.Default_NonSecurePort, opts.NonSecurePort);
            Assert.Equal(EurekaInstanceOptions.Default_SecurePort, opts.SecurePort);
            Assert.True(opts.IsNonSecurePortEnabled);
            Assert.False(opts.SecurePortEnabled);
            Assert.Equal(EurekaInstanceOptions.Default_LeaseRenewalIntervalInSeconds, opts.LeaseRenewalIntervalInSeconds);
            Assert.Equal(EurekaInstanceOptions.Default_LeaseExpirationDurationInSeconds, opts.LeaseExpirationDurationInSeconds);
            Assert.Null(opts.VirtualHostName);
            Assert.Null(opts.SecureVirtualHostName);
            Assert.Null(opts.ASGName);
            Assert.NotNull(opts.MetadataMap);
            Assert.Equal(0, opts.MetadataMap.Count);
            Assert.Equal(EurekaInstanceOptions.Default_StatusPageUrlPath, opts.StatusPageUrlPath);
            Assert.Null(opts.StatusPageUrl);
            Assert.Equal(EurekaInstanceOptions.Default_HomePageUrlPath, opts.HomePageUrlPath);
            Assert.Null(opts.HomePageUrl);
            Assert.Equal(EurekaInstanceOptions.Default_HealthCheckUrlPath, opts.HealthCheckUrlPath);
            Assert.Null(opts.HealthCheckUrl);
            Assert.Null(opts.SecureHealthCheckUrl);
            Assert.Equal(DataCenterName.MyOwn, opts.DataCenterInfo.Name);
            Assert.Equal(opts.GetHostAddress(false), opts.IpAddress);
            Assert.Null(opts.DefaultAddressResolutionOrder);

        }

    }
}
