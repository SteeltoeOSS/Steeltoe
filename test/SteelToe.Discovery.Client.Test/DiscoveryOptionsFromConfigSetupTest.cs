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

using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace SteelToe.Discovery.Client.Test
{
    public class DiscoveryOptionsFromConfigSetupTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            IConfiguration config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new DiscoveryOptionsFromConfigSetup(config));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void ConfigureDiscovery_ConfiguresEurekaDiscovery_Correctly()
        {
            // Arrange
            var appsettings = @"
{
'eureka': {
    'client': {
        'eurekaServer': {
            'proxyHost': 'proxyHost',
            'proxyPort': 100,
            'proxyUserName': 'proxyUserName',
            'proxyPassword': 'proxyPassword',
            'shouldGZipContent': true,
            'connectTimeoutSeconds': 100
        },
        'allowRedirects': true,
        'shouldDisableDelta': true,
        'shouldFilterOnlyUpInstances': true,
        'shouldFetchRegistry': true,
        'registryRefreshSingleVipAddress':'registryRefreshSingleVipAddress',
        'shouldOnDemandUpdateStatusChange': true,
        'shouldRegisterWithEureka': true,
        'registryFetchIntervalSeconds': 100,
        'instanceInfoReplicationIntervalSeconds': 100,
        'serviceUrl': 'http://localhost:8761/eureka/'
    },
    'instance': {
        'instanceId': 'instanceId',
        'appName': 'appName',
        'appGroup': 'appGroup',
        'instanceEnabledOnInit': true,
        'port': 100,
        'securePort': 100,
        'nonSecurePortEnabled': true,
        'securePortEnabled': true,
        'leaseExpirationDurationInSeconds':100,
        'leaseRenewalIntervalInSeconds': 100,
        'secureVipAddress': 'secureVipAddress',
        'vipAddress': 'vipAddress',
        'asgName': 'asgName',
        'metadataMap': {
            'foo': 'bar',
            'bar': 'foo'
        },
        'statusPageUrlPath': 'statusPageUrlPath',
        'statusPageUrl': 'statusPageUrl',
        'homePageUrlPath':'homePageUrlPath',
        'homePageUrl': 'homePageUrl',
        'healthCheckUrlPath': 'healthCheckUrlPath',
        'healthCheckUrl':'healthCheckUrl',
        'secureHealthCheckUrl':'secureHealthCheckUrl'   
    }
    }
}";

            var path = TestHelpers.CreateTempFile(appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(path);
            var config = configurationBuilder.Build();

            var options = new DiscoveryOptions();
            DiscoveryOptionsFromConfigSetup.ConfigureDiscovery(config, options);
            Assert.Equal(DiscoveryClientType.EUREKA, options.ClientType);

            var co = options.ClientOptions as EurekaClientOptions;
            Assert.NotNull(co);
            Assert.Equal("proxyHost", co.ProxyHost);
            Assert.Equal(100, co.ProxyPort);
            Assert.Equal("proxyPassword", co.ProxyPassword);
            Assert.Equal("proxyUserName", co.ProxyUserName);
            Assert.True(co.AllowRedirects);
            Assert.Equal(100, co.InstanceInfoReplicationIntervalSeconds);
            Assert.Equal(100, co.EurekaServerConnectTimeoutSeconds);
            Assert.Equal("http://localhost:8761/eureka/", co.EurekaServerServiceUrls);
            Assert.Equal(100, co.RegistryFetchIntervalSeconds);
            Assert.Equal("registryRefreshSingleVipAddress", co.RegistryRefreshSingleVipAddress);
            Assert.True(co.ShouldDisableDelta);
            Assert.True(co.ShouldFetchRegistry);
            Assert.True(co.ShouldFilterOnlyUpInstances);
            Assert.True(co.ShouldGZipContent);
            Assert.True(co.ShouldOnDemandUpdateStatusChange);
            Assert.True(co.ShouldRegisterWithEureka);

            var ro = options.RegistrationOptions as EurekaInstanceOptions;
            Assert.NotNull(ro);


            Assert.Equal("instanceId", ro.InstanceId);
            Assert.Equal("appName", ro.AppName);
            Assert.Equal("appGroup", ro.AppGroupName);
            Assert.True(ro.IsInstanceEnabledOnInit);
            Assert.Equal(100, ro.NonSecurePort);
            Assert.Equal(100, ro.SecurePort);
            Assert.True(ro.IsNonSecurePortEnabled);
            Assert.True(ro.SecurePortEnabled);
            Assert.Equal(100, ro.LeaseExpirationDurationInSeconds);
            Assert.Equal(100, ro.LeaseRenewalIntervalInSeconds);
            Assert.Equal("secureVipAddress", ro.SecureVirtualHostName);
            Assert.Equal("vipAddress", ro.VirtualHostName);
            Assert.Equal("asgName", ro.ASGName);

            Assert.Equal("statusPageUrlPath", ro.StatusPageUrlPath);
            Assert.Equal("statusPageUrl", ro.StatusPageUrl);
            Assert.Equal("homePageUrlPath", ro.HomePageUrlPath);
            Assert.Equal("homePageUrl", ro.HomePageUrl);
            Assert.Equal("healthCheckUrlPath", ro.HealthCheckUrlPath);
            Assert.Equal("healthCheckUrl", ro.HealthCheckUrl);
            Assert.Equal("secureHealthCheckUrl", ro.SecureHealthCheckUrl);

            var map = ro.MetadataMap;
            Assert.NotNull(map);
            Assert.Equal(2, map.Count);
            Assert.Equal("bar", map["foo"]);
            Assert.Equal("foo", map["bar"]);

        }
    }
}
