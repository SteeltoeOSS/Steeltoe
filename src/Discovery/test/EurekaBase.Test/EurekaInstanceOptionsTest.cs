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

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Discovery.Eureka.AppInfo;
using System.IO;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaInstanceOptionsTest : AbstractBaseTest
    {
        [Fact]
        [Trait("Category", "SkipOnMacOS")]
        public void Constructor_Intializes_Defaults()
        {
            var opts = new EurekaInstanceOptions();
            Assert.NotNull(opts.InstanceId);
            Assert.Equal("unknown", opts.AppName);
            Assert.Null(opts.AppGroupName);
            Assert.True(opts.IsInstanceEnabledOnInit);
            Assert.Equal(80, opts.NonSecurePort);
            Assert.Equal(443, opts.SecurePort);
            Assert.True(opts.IsNonSecurePortEnabled);
            Assert.False(opts.SecurePortEnabled);
            Assert.Equal(EurekaInstanceOptions.Default_LeaseRenewalIntervalInSeconds, opts.LeaseRenewalIntervalInSeconds);
            Assert.Equal(EurekaInstanceOptions.Default_LeaseExpirationDurationInSeconds, opts.LeaseExpirationDurationInSeconds);
            Assert.Null(opts.VirtualHostName);
            Assert.Null(opts.SecureVirtualHostName);
            Assert.Null(opts.ASGName);
            Assert.NotNull(opts.MetadataMap);
            Assert.Empty(opts.MetadataMap);
            Assert.Equal(EurekaInstanceOptions.Default_StatusPageUrlPath, opts.StatusPageUrlPath);
            Assert.Null(opts.StatusPageUrl);
            Assert.Equal(EurekaInstanceOptions.Default_HomePageUrlPath, opts.HomePageUrlPath);
            Assert.Null(opts.HomePageUrl);
            Assert.Equal(EurekaInstanceOptions.Default_HealthCheckUrlPath, opts.HealthCheckUrlPath);
            Assert.Null(opts.HealthCheckUrl);
            Assert.Null(opts.SecureHealthCheckUrl);
            Assert.Equal(DataCenterName.MyOwn, opts.DataCenterInfo.Name);
            Assert.NotNull(opts.IpAddress); // TODO: this is null on MacOS
            Assert.Equal(opts.GetHostAddress(false), opts.IpAddress);
            Assert.Null(opts.DefaultAddressResolutionOrder);
            Assert.Null(opts.RegistrationMethod);
        }

        [Fact]
        public void Constructor_ConfiguresEurekaDiscovery_Correctly()
        {
            // Arrange
            var appsettings = @"
                {
                    ""eureka"": {
                        ""client"": {
                            ""eurekaServer"": {
                                ""proxyHost"": ""proxyHost"",
                                ""proxyPort"": 100,
                                ""proxyUserName"": ""proxyUserName"",
                                ""proxyPassword"": ""proxyPassword"",
                                ""shouldGZipContent"": true,
                                ""connectTimeoutSeconds"": 100
                            },
                            ""allowRedirects"": true,
                            ""shouldDisableDelta"": true,
                            ""shouldFilterOnlyUpInstances"": true,
                            ""shouldFetchRegistry"": true,
                            ""registryRefreshSingleVipAddress"":""registryRefreshSingleVipAddress"",
                            ""shouldOnDemandUpdateStatusChange"": true,
                            ""shouldRegisterWithEureka"": true,
                            ""registryFetchIntervalSeconds"": 100,
                            ""instanceInfoReplicationIntervalSeconds"": 100,
                            ""serviceUrl"": ""http://localhost:8761/eureka/""
                        },
                        ""instance"": {
                            ""registrationMethod"" : ""foobar"",
                            ""hostName"": ""myHostName"",
                            ""instanceId"": ""instanceId"",
                            ""appName"": ""appName"",
                            ""appGroup"": ""appGroup"",
                            ""instanceEnabledOnInit"": true,
                            ""port"": 100,
                            ""securePort"": 100,
                            ""nonSecurePortEnabled"": true,
                            ""securePortEnabled"": true,
                            ""leaseExpirationDurationInSeconds"":100,
                            ""leaseRenewalIntervalInSeconds"": 100,
                            ""secureVipAddress"": ""secureVipAddress"",
                            ""vipAddress"": ""vipAddress"",
                            ""asgName"": ""asgName"",
                            ""metadataMap"": {
                                ""foo"": ""bar"",
                                ""bar"": ""foo""
                            },
                            ""statusPageUrlPath"": ""statusPageUrlPath"",
                            ""statusPageUrl"": ""statusPageUrl"",
                            ""homePageUrlPath"":""homePageUrlPath"",
                            ""homePageUrl"": ""homePageUrl"",
                            ""healthCheckUrlPath"": ""healthCheckUrlPath"",
                            ""healthCheckUrl"":""healthCheckUrl"",
                            ""secureHealthCheckUrl"":""secureHealthCheckUrl""   
                        }
                    }
                }";
            var path = TestHelpers.CreateTempFile(appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            var config = configurationBuilder.Build();

            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            var ro = new EurekaInstanceOptions();
            instSection.Bind(ro);

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
            Assert.Equal("myHostName", ro.GetHostName(false));
            Assert.Equal("myHostName", ro.HostName);
            Assert.Equal("foobar", ro.RegistrationMethod);
            var map = ro.MetadataMap;
            Assert.NotNull(map);
            Assert.Equal(2, map.Count);
            Assert.Equal("bar", map["foo"]);
            Assert.Equal("foo", map["bar"]);
        }
    }
}
