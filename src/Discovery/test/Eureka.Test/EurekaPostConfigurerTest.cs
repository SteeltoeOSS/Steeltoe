// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test
{
    public class EurekaPostConfigurerTest
    {
        public EurekaPostConfigurerTest()
        {
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
        }

        [Fact]
        public void UpdateConfiguration_WithInstDefaults_UpdatesCorrectly()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "bar" },
                { "spring:application:instance_id", "instance" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            var root = builder.Build();

            var instOpts = new EurekaInstanceOptions();
            EurekaPostConfigurer.UpdateConfiguration(root, instOpts, new ApplicationInstanceInfo(root));

            Assert.Equal("bar", instOpts.AppName);
            Assert.Equal("instance", instOpts.InstanceId);
            Assert.Equal("registrationMethod", instOpts.RegistrationMethod);
            Assert.Equal("bar", instOpts.VirtualHostName);
            Assert.Equal("bar", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_UpdatesCorrectly()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "bar" },
                { "spring:application:instance_id", "instance" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            var root = builder.Build();

            var instOpts = new EurekaInstanceOptions()
            {
                AppName = "dontChange",
                InstanceId = "dontChange",
                RegistrationMethod = "dontChange"
            };

            EurekaPostConfigurer.UpdateConfiguration(root, instOpts, null);

            Assert.Equal("dontChange", instOpts.AppName);
            Assert.Equal("dontChange", instOpts.InstanceId);
            Assert.Equal("dontChange", instOpts.RegistrationMethod);
            Assert.Equal("dontChange", instOpts.VirtualHostName);
            Assert.Equal("dontChange", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_UpdatesCorrectly2()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:name", "bar" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            var root = builder.Build();

            var instOpts = new EurekaInstanceOptions();

            EurekaPostConfigurer.UpdateConfiguration(root, instOpts, new ApplicationInstanceInfo(root));

            Assert.Equal("bar", instOpts.AppName);
            Assert.EndsWith("bar:80", instOpts.InstanceId, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("registrationMethod", instOpts.RegistrationMethod);
            Assert.Equal("bar", instOpts.VirtualHostName);
            Assert.Equal("bar", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_UpdatesCorrectly3()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "spring:application:instance_id", "instance" },
                { "spring:cloud:discovery:registrationMethod", "registrationMethod" },
            });

            var root = builder.Build();

            var instOpts = new EurekaInstanceOptions()
            {
                AppName = "dontChange",
                InstanceId = "dontChange",
                RegistrationMethod = "dontChange"
            };

            EurekaPostConfigurer.UpdateConfiguration(root, instOpts, null);

            Assert.Equal("dontChange", instOpts.AppName);
            Assert.Equal("dontChange", instOpts.InstanceId);
            Assert.Equal("dontChange", instOpts.RegistrationMethod);
            Assert.Equal("dontChange", instOpts.VirtualHostName);
            Assert.Equal("dontChange", instOpts.SecureVirtualHostName);
        }

        [Fact]
        public void UpdateConfiguration_NoServiceInfo_ConfiguresEurekaDiscovery_Correctly()
        {
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
                            ""instanceId"": ""instanceId"",
                            ""appName"": ""appName"",
                            ""appGroup"": ""appGroup"",
                            ""instanceEnabledOnInit"": true,
                            ""hostname"": ""hostname"",
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
            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(sandbox.FullPath);
            configurationBuilder.AddJsonFile(Path.GetFileName(path));
            var config = configurationBuilder.Build();

            var clientOpts = new EurekaClientOptions();
            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            clientSection.Bind(clientOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, null, clientOpts);

            var co = clientOpts;
            Assert.NotNull(co);
            Assert.Equal("proxyHost", co.ProxyHost);
            Assert.Equal(100, co.ProxyPort);
            Assert.Equal("proxyPassword", co.ProxyPassword);
            Assert.Equal("proxyUserName", co.ProxyUserName);
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

            var instOpts = new EurekaInstanceOptions();
            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            instSection.Bind(instOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, null, instOpts, null);

            var ro = instOpts;

            Assert.Equal("instanceId", ro.InstanceId);
            Assert.Equal("appName", ro.AppName);
            Assert.Equal("appGroup", ro.AppGroupName);
            Assert.True(ro.IsInstanceEnabledOnInit);
            Assert.Equal(100, ro.NonSecurePort);
            Assert.Equal("hostname", ro.HostName);
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

        [Fact]
        public void UpdateConfigurationComplainsAboutDefaultWhenWontWork()
        {
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

            var exception = Assert.Throws<InvalidOperationException>(() => EurekaPostConfigurer.UpdateConfiguration(null, null, new EurekaClientOptions()));
            Assert.Contains(EurekaClientConfig.Default_ServerServiceUrl, exception.Message);
            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        }

        [Fact]
        public void UpdateConfiguration_WithVCAPEnvVariables_HostName_ConfiguresEurekaDiscovery_Correctly()
        {
            var vcap_application = @"
                {
                    ""limits"": {
                        ""fds"": 16384,
                        ""mem"": 512,
                        ""disk"": 1024
                    },
                    ""application_name"": ""foo"",
                    ""application_uris"": [
                        ""foo.apps.testcloud.com""
                    ],
                    ""name"": ""foo"",
                    ""space_name"": ""test"",
                    ""space_id"": ""98c627e7-f559-46a4-9032-88cab63f8249"",
                    ""uris"": [
                        ""foo.apps.testcloud.com""
                    ],
                    ""users"": null,
                    ""version"": ""4a439db9-4a82-47a3-aeea-8240465cff8e"",
                    ""application_version"": ""4a439db9-4a82-47a3-aeea-8240465cff8e"",
                    ""application_id"": ""ac923014-93a5-4aee-b934-a043b241868b"",
                    ""instance_id"": ""instance_id""
                }";
            var vcap_services = @"
                {
                    ""p-config-server"": [{
                        ""credentials"": {
                            ""uri"": ""https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.testcloud.com"",
                            ""client_id"": ""p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333"",
                            ""client_secret"": ""vBDjqIf7XthT"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-config-server"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myConfigServer"",
                        ""tags"": [
                            ""configuration"",
                            ""spring-cloud""
                        ]
                    }],
                    ""p-service-registry"": [
                    {
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                            },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService"",
                        ""tags"": [
                        ""eureka"",
                        ""discovery"",
                        ""registry"",
                        ""spring-cloud""
                        ]
                    }]
                }";

            var appsettings = @"
                {
                    ""spring"": {
                        ""cloud"": {
                            ""discovery"": {
                                ""registrationMethod"": ""hostname""
                            }
                        }
                    },
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
                            ""instanceId"": ""instanceId"",
                            ""appGroup"": ""appGroup"",
                            ""instanceEnabledOnInit"": true,
                            ""hostname"": ""myhostname"",
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
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);
            Environment.SetEnvironmentVariable("CF_INSTANCE_INDEX", "1");
            Environment.SetEnvironmentVariable("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            var sis = config.GetServiceInfos<EurekaServiceInfo>();
            Assert.Single(sis);
            var si = sis.First();

            var clientOpts = new EurekaClientOptions();
            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            clientSection.Bind(clientOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, si, clientOpts);

            var co = clientOpts;
            Assert.NotNull(co);
            Assert.Equal("proxyHost", co.ProxyHost);
            Assert.Equal(100, co.ProxyPort);
            Assert.Equal("proxyPassword", co.ProxyPassword);
            Assert.Equal("proxyUserName", co.ProxyUserName);
            Assert.Equal(100, co.EurekaServerConnectTimeoutSeconds);
            Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", co.EurekaServerServiceUrls);
            Assert.Equal(100, co.RegistryFetchIntervalSeconds);
            Assert.Equal("registryRefreshSingleVipAddress", co.RegistryRefreshSingleVipAddress);
            Assert.True(co.ShouldDisableDelta);
            Assert.True(co.ShouldFetchRegistry);
            Assert.True(co.ShouldFilterOnlyUpInstances);
            Assert.True(co.ShouldGZipContent);
            Assert.True(co.ShouldOnDemandUpdateStatusChange);
            Assert.True(co.ShouldRegisterWithEureka);
            Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", co.AccessTokenUri);
            Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", co.ClientId);
            Assert.Equal("dCsdoiuklicS", co.ClientSecret);

            var instOpts = new EurekaInstanceOptions();
            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            instSection.Bind(instOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, si, instOpts, si.ApplicationInfo);

            var ro = instOpts;

            Assert.Equal("hostname", ro.RegistrationMethod);
            Assert.Equal("myhostname:instance_id", ro.InstanceId);
            Assert.Equal("foo", ro.AppName);
            Assert.Equal("appGroup", ro.AppGroupName);
            Assert.True(ro.IsInstanceEnabledOnInit);
            Assert.Equal(100, ro.NonSecurePort);
            Assert.Equal("myhostname", ro.HostName);
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
            Assert.Equal(6, map.Count);
            Assert.Equal("bar", map["foo"]);
            Assert.Equal("foo", map["bar"]);
            Assert.Equal("instance_id", map[EurekaPostConfigurer.INSTANCE_ID]);
            Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", map[EurekaPostConfigurer.CF_APP_GUID]);
            Assert.Equal("1", map[EurekaPostConfigurer.CF_INSTANCE_INDEX]);
            Assert.Equal(EurekaPostConfigurer.UNKNOWN_ZONE, map[EurekaPostConfigurer.ZONE]);
        }

        [Fact]
        public void UpdateConfiguration_WithVCAPEnvVariables_Route_ConfiguresEurekaDiscovery_Correctly()
        {
            var vcap_application = @"
                {
                    ""limits"": {
                        ""fds"": 16384,
                        ""mem"": 512,
                        ""disk"": 1024
                    },
                    ""application_name"": ""foo"",
                    ""application_uris"": [
                        ""foo.apps.testcloud.com""
                    ],
                    ""name"": ""foo"",
                    ""space_name"": ""test"",
                    ""space_id"": ""98c627e7-f559-46a4-9032-88cab63f8249"",
                    ""uris"": [
                        ""foo.apps.testcloud.com""
                    ],
                    ""users"": null,
                    ""version"": ""4a439db9-4a82-47a3-aeea-8240465cff8e"",
                    ""application_version"": ""4a439db9-4a82-47a3-aeea-8240465cff8e"",
                    ""application_id"": ""ac923014-93a5-4aee-b934-a043b241868b"",
                    ""instance_id"": ""instance_id""
                }";
            var vcap_services = @"
                {
                    ""p-config-server"": [{
                        ""credentials"": {
                            ""uri"": ""https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.testcloud.com"",
                            ""client_id"": ""p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333"",
                            ""client_secret"": ""vBDjqIf7XthT"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-config-server"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myConfigServer"",
                        ""tags"": [
                            ""configuration"",
                            ""spring-cloud""
                        ]
                    }],
                    ""p-service-registry"": [{
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                            },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    }]
                }";

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
                            ""registrationMethod"": ""route"",
                            ""instanceId"": ""instanceId"",
                            ""appGroup"": ""appGroup"",
                            ""instanceEnabledOnInit"": true,
                            ""hostname"": ""myhostname"",
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
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);
            Environment.SetEnvironmentVariable("CF_INSTANCE_INDEX", "1");
            Environment.SetEnvironmentVariable("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");
            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            var sis = config.GetServiceInfos<EurekaServiceInfo>();
            Assert.Single(sis);
            var si = sis.First();

            var clientOpts = new EurekaClientOptions();
            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            clientSection.Bind(clientOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, si, clientOpts);

            var co = clientOpts;
            Assert.NotNull(co);
            Assert.Equal("proxyHost", co.ProxyHost);
            Assert.Equal(100, co.ProxyPort);
            Assert.Equal("proxyPassword", co.ProxyPassword);
            Assert.Equal("proxyUserName", co.ProxyUserName);
            Assert.Equal(100, co.EurekaServerConnectTimeoutSeconds);
            Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", co.EurekaServerServiceUrls);
            Assert.Equal(100, co.RegistryFetchIntervalSeconds);
            Assert.Equal("registryRefreshSingleVipAddress", co.RegistryRefreshSingleVipAddress);
            Assert.True(co.ShouldDisableDelta);
            Assert.True(co.ShouldFetchRegistry);
            Assert.True(co.ShouldFilterOnlyUpInstances);
            Assert.True(co.ShouldGZipContent);
            Assert.True(co.ShouldOnDemandUpdateStatusChange);
            Assert.True(co.ShouldRegisterWithEureka);
            Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", co.AccessTokenUri);
            Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", co.ClientId);
            Assert.Equal("dCsdoiuklicS", co.ClientSecret);

            var instOpts = new EurekaInstanceOptions();
            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            instSection.Bind(instOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, si, instOpts, si.ApplicationInfo);

            var ro = instOpts;

            Assert.Equal("route", ro.RegistrationMethod);
            Assert.Equal("foo.apps.testcloud.com:instance_id", ro.InstanceId);
            Assert.Equal("foo", ro.AppName);
            Assert.Equal("appGroup", ro.AppGroupName);
            Assert.True(ro.IsInstanceEnabledOnInit);
            Assert.Equal(80, ro.NonSecurePort);
            Assert.Equal("foo.apps.testcloud.com", ro.HostName);
            Assert.Equal(443, ro.SecurePort);
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
            Assert.Equal(6, map.Count);
            Assert.Equal("bar", map["foo"]);
            Assert.Equal("foo", map["bar"]);
            Assert.Equal("instance_id", map[EurekaPostConfigurer.INSTANCE_ID]);
            Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", map[EurekaPostConfigurer.CF_APP_GUID]);
            Assert.Equal("1", map[EurekaPostConfigurer.CF_INSTANCE_INDEX]);
            Assert.Equal(EurekaPostConfigurer.UNKNOWN_ZONE, map[EurekaPostConfigurer.ZONE]);
        }

        [Fact]
        public void UpdateConfiguration_WithVCAPEnvVariables_AppName_Overrides_VCAPBinding()
        {
            var vcap_application = @"
                {
                    ""limits"": {
                        ""fds"": 16384,
                        ""mem"": 512,
                        ""disk"": 1024
                    },
                    ""application_name"": ""foo"",
                    ""application_uris"": [
                        ""foo.apps.testcloud.com""
                    ],
                    ""name"": ""foo"",
                    ""space_name"": ""test"",
                    ""space_id"": ""98c627e7-f559-46a4-9032-88cab63f8249"",
                    ""uris"": [
                        ""foo.apps.testcloud.com""
                    ],
                    ""users"": null,
                    ""version"": ""4a439db9-4a82-47a3-aeea-8240465cff8e"",
                    ""application_version"": ""4a439db9-4a82-47a3-aeea-8240465cff8e"",
                    ""application_id"": ""ac923014-93a5-4aee-b934-a043b241868b"",
                    ""instance_id"": ""instance_id""
                }";
            var vcap_services = @"
                {
                    ""p-config-server"": [{
                        ""credentials"": {
                            ""uri"": ""https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.testcloud.com"",
                            ""client_id"": ""p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333"",
                            ""client_secret"": ""vBDjqIf7XthT"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-config-server"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myConfigServer"",
                        ""tags"": [
                            ""configuration"",
                            ""spring-cloud""
                        ]
                    }],
                    ""p-service-registry"": [{
                        ""credentials"": {
                            ""uri"": ""https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com"",
                            ""client_id"": ""p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe"",
                            ""client_secret"": ""dCsdoiuklicS"",
                            ""access_token_uri"": ""https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token""
                        },
                        ""syslog_drain_url"": null,
                        ""label"": ""p-service-registry"",
                        ""provider"": null,
                        ""plan"": ""standard"",
                        ""name"": ""myDiscoveryService"",
                        ""tags"": [
                            ""eureka"",
                            ""discovery"",
                            ""registry"",
                            ""spring-cloud""
                        ]
                    }]
                }";

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
                            ""registrationMethod"": ""hostname"",
                            ""instanceId"": ""instanceId"",
                            ""appName"": ""appName"",
                            ""appGroup"": ""appGroup"",
                            ""instanceEnabledOnInit"": true,
                            ""hostname"": ""myhostname"",
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
            Environment.SetEnvironmentVariable("VCAP_APPLICATION", vcap_application);
            Environment.SetEnvironmentVariable("VCAP_SERVICES", vcap_services);
            Environment.SetEnvironmentVariable("CF_INSTANCE_INDEX", "1");
            Environment.SetEnvironmentVariable("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");
            using var sandbox = new Sandbox();
            var path = sandbox.CreateFile("appsettings.json", appsettings);
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileName(path);
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(directory);

            configurationBuilder.AddJsonFile(fileName);
            configurationBuilder.AddCloudFoundry();
            var config = configurationBuilder.Build();

            var sis = config.GetServiceInfos<EurekaServiceInfo>();
            Assert.Single(sis);
            var si = sis.First();

            var clientOpts = new EurekaClientOptions();
            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            clientSection.Bind(clientOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, si, clientOpts);

            var co = clientOpts;

            Assert.NotNull(co);
            Assert.Equal("proxyHost", co.ProxyHost);
            Assert.Equal(100, co.ProxyPort);
            Assert.Equal("proxyPassword", co.ProxyPassword);
            Assert.Equal("proxyUserName", co.ProxyUserName);
            Assert.Equal(100, co.EurekaServerConnectTimeoutSeconds);
            Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", co.EurekaServerServiceUrls);
            Assert.Equal(100, co.RegistryFetchIntervalSeconds);
            Assert.Equal("registryRefreshSingleVipAddress", co.RegistryRefreshSingleVipAddress);
            Assert.True(co.ShouldDisableDelta);
            Assert.True(co.ShouldFetchRegistry);
            Assert.True(co.ShouldFilterOnlyUpInstances);
            Assert.True(co.ShouldGZipContent);
            Assert.True(co.ShouldOnDemandUpdateStatusChange);
            Assert.True(co.ShouldRegisterWithEureka);
            Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", co.AccessTokenUri);
            Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", co.ClientId);
            Assert.Equal("dCsdoiuklicS", co.ClientSecret);

            var instOpts = new EurekaInstanceOptions();
            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            instSection.Bind(instOpts);

            EurekaPostConfigurer.UpdateConfiguration(config, si, instOpts, si.ApplicationInfo);

            var ro = instOpts;

            Assert.Equal("hostname", ro.RegistrationMethod);
            Assert.Equal("myhostname:instance_id", ro.InstanceId);
            Assert.Equal("appName", ro.AppName);
            Assert.Equal("appGroup", ro.AppGroupName);
            Assert.True(ro.IsInstanceEnabledOnInit);
            Assert.Equal(100, ro.NonSecurePort);
            Assert.Equal("myhostname", ro.HostName);
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
            Assert.Equal(6, map.Count);
            Assert.Equal("bar", map["foo"]);
            Assert.Equal("foo", map["bar"]);
            Assert.Equal("instance_id", map[EurekaPostConfigurer.INSTANCE_ID]);
            Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", map[EurekaPostConfigurer.CF_APP_GUID]);
            Assert.Equal("1", map[EurekaPostConfigurer.CF_INSTANCE_INDEX]);
            Assert.Equal(EurekaPostConfigurer.UNKNOWN_ZONE, map[EurekaPostConfigurer.ZONE]);
        }

        [Fact]
        public void UpdateConfigurationFindsUrls()
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>() { { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" } }).Build();
            var instOpts = new EurekaInstanceOptions();
            var appInfo = new ApplicationInstanceInfo(config);

            EurekaPostConfigurer.UpdateConfiguration(config, instOpts, appInfo);

            Assert.Equal("myapp", instOpts.HostName);
            Assert.Equal(1234, instOpts.SecurePort);
            Assert.Equal(1233, instOpts.Port);
        }

        [Fact]
        public void UpdateConfiguration_DisableClientShouldNotComplainAboutInvalidConfiguration()
        {

            var clientOptions = new EurekaClientOptions()
            {
                Enabled = false
            };

            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", "true");

            var ex = Record.Exception(() => EurekaPostConfigurer.UpdateConfiguration(null, null, clientOptions));
            Assert.Null(ex);

            Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", null);
        }
    }
}
