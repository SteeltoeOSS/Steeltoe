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

using Microsoft.Extensions.Configuration;
using System.IO;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaClientOptionsTest : AbstractBaseTest
    {
        [Fact]
        public void Constructor_Intializes_Defaults()
        {
            EurekaClientOptions opts = new EurekaClientOptions();
            Assert.True(opts.Enabled);
            Assert.Equal(EurekaClientOptions.Default_RegistryFetchIntervalSeconds, opts.RegistryFetchIntervalSeconds);
            Assert.Equal(EurekaClientOptions.Default_InstanceInfoReplicationIntervalSeconds, opts.InstanceInfoReplicationIntervalSeconds);
            Assert.Null(opts.ProxyHost);
            Assert.Equal(0, opts.ProxyPort);
            Assert.Null(opts.ProxyUserName);
            Assert.Null(opts.ProxyPassword);
            Assert.True(opts.ShouldGZipContent);
            Assert.Equal(EurekaClientOptions.Default_EurekaServerConnectTimeoutSeconds, opts.EurekaServerConnectTimeoutSeconds);
            Assert.True(opts.ShouldRegisterWithEureka);
            Assert.False(opts.AllowRedirects);
            Assert.False(opts.ShouldDisableDelta);
            Assert.True(opts.ShouldFilterOnlyUpInstances);
            Assert.True(opts.ShouldFetchRegistry);
            Assert.Null(opts.RegistryRefreshSingleVipAddress);
            Assert.True(opts.ShouldOnDemandUpdateStatusChange);
            Assert.Equal(EurekaClientOptions.Default_ServerServiceUrl, opts.EurekaServerServiceUrls);
        }

        [Fact]
        public void Constructor_ConfiguresEurekaDiscovery_Correctly()
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
        'serviceUrl': 'http://foo.bar:8761/eureka/'
    },
    'instance': {
        'registrationMethod' : 'foobar',
        'hostName': 'myHostName',
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
            string directory = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            var co = new EurekaClientOptions();
            clientSection.Bind(co);

            Assert.Equal("proxyHost", co.ProxyHost);
            Assert.Equal(100, co.ProxyPort);
            Assert.Equal("proxyPassword", co.ProxyPassword);
            Assert.Equal("proxyUserName", co.ProxyUserName);
            Assert.True(co.AllowRedirects);
            Assert.Equal(100, co.InstanceInfoReplicationIntervalSeconds);
            Assert.Equal(100, co.EurekaServerConnectTimeoutSeconds);
            Assert.Equal("http://foo.bar:8761/eureka/", co.EurekaServerServiceUrls);
            Assert.Equal(100, co.RegistryFetchIntervalSeconds);
            Assert.Equal("registryRefreshSingleVipAddress", co.RegistryRefreshSingleVipAddress);
            Assert.True(co.ShouldDisableDelta);
            Assert.True(co.ShouldFetchRegistry);
            Assert.True(co.ShouldFilterOnlyUpInstances);
            Assert.True(co.ShouldGZipContent);
            Assert.True(co.ShouldOnDemandUpdateStatusChange);
            Assert.True(co.ShouldRegisterWithEureka);
        }
    }
}
