// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBindings;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class CloudFoundryTest
{
    [Fact]
    public async Task NoVCAPEnvVariables_ConfiguresEurekaDiscovery_Correctly()
    {
        const string appSettings = """
            {
              "eureka": {
                "client": {
                  "eurekaServer": {
                    "proxyHost": "proxyHost",
                    "proxyPort": 100,
                    "proxyUserName": "proxyUserName",
                    "proxyPassword": "proxyPassword",
                    "shouldGZipContent": true,
                    "connectTimeoutSeconds": 100
                  },
                  "allowRedirects": true,
                  "shouldDisableDelta": true,
                  "shouldFilterOnlyUpInstances": true,
                  "shouldFetchRegistry": false,
                  "registryRefreshSingleVipAddress": "registryRefreshSingleVipAddress",
                  "shouldRegisterWithEureka": false,
                  "registryFetchIntervalSeconds": 100,
                  "instanceInfoReplicationIntervalSeconds": 100,
                  "serviceUrl": "http://api.eureka.com:8761/eureka/"
                },
                "instance": {
                  "instanceId": "instanceId",
                  "appName": "appName",
                  "appGroup": "appGroup",
                  "instanceEnabledOnInit": true,
                  "hostname": "hostname",
                  "port": 100,
                  "securePort": 100,
                  "nonSecurePortEnabled": true,
                  "securePortEnabled": true,
                  "leaseExpirationDurationInSeconds": 100,
                  "leaseRenewalIntervalInSeconds": 100,
                  "secureVipAddress": "secureVipAddress",
                  "vipAddress": "vipAddress",
                  "asgName": "asgName",
                  "metadataMap": {
                    "foo": "bar",
                    "bar": "foo"
                  },
                  "statusPageUrlPath": "statusPageUrlPath",
                  "statusPageUrl": "statusPageUrl",
                  "homePageUrlPath": "homePageUrlPath",
                  "homePageUrl": "homePageUrl",
                  "healthCheckUrlPath": "healthCheckUrlPath",
                  "healthCheckUrl": "healthCheckUrl",
                  "secureHealthCheckUrl": "secureHealthCheckUrl"
                }
              }
            }
            """;

        await using var stream = TextConverter.ToStream(appSettings);
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        clientOptions.EurekaServer.ProxyHost.Should().Be("proxyHost");
        clientOptions.EurekaServer.ProxyPort.Should().Be(100);
        clientOptions.EurekaServer.ProxyPassword.Should().Be("proxyPassword");
        clientOptions.EurekaServer.ProxyUserName.Should().Be("proxyUserName");
        clientOptions.EurekaServer.ConnectTimeoutSeconds.Should().Be(100);
        clientOptions.EurekaServerServiceUrls.Should().Be("http://api.eureka.com:8761/eureka/");
        clientOptions.RegistryFetchIntervalSeconds.Should().Be(100);
        clientOptions.RegistryRefreshSingleVipAddress.Should().Be("registryRefreshSingleVipAddress");
        clientOptions.IsFetchDeltaDisabled.Should().BeTrue();
        clientOptions.ShouldFetchRegistry.Should().BeFalse();
        clientOptions.ShouldFilterOnlyUpInstances.Should().BeTrue();
        clientOptions.EurekaServer.ShouldGZipContent.Should().BeTrue();
        clientOptions.ShouldRegisterWithEureka.Should().BeFalse();

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        instanceOptions.InstanceId.Should().Be("instanceId");
        instanceOptions.AppName.Should().Be("appName");
        instanceOptions.AppGroupName.Should().Be("appGroup");
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(100);
        instanceOptions.HostName.Should().Be("hostname");
        instanceOptions.SecurePort.Should().Be(100);
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(100);
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(100);
        instanceOptions.SecureVipAddress.Should().Be("secureVipAddress");
        instanceOptions.VipAddress.Should().Be("vipAddress");
        instanceOptions.AutoScalingGroupName.Should().Be("asgName");

        instanceOptions.StatusPageUrlPath.Should().Be("statusPageUrlPath");
        instanceOptions.StatusPageUrl.Should().Be("statusPageUrl");
        instanceOptions.HomePageUrlPath.Should().Be("homePageUrlPath");
        instanceOptions.HomePageUrl.Should().Be("homePageUrl");
        instanceOptions.HealthCheckUrlPath.Should().Be("healthCheckUrlPath");
        instanceOptions.HealthCheckUrl.Should().Be("healthCheckUrl");
        instanceOptions.SecureHealthCheckUrl.Should().Be("secureHealthCheckUrl");

        instanceOptions.MetadataMap.Should().HaveCount(2);
        instanceOptions.MetadataMap.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instanceOptions.MetadataMap.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
    }

    [Fact]
    public async Task WithVCAPEnvVariables_HostName_ConfiguresEurekaDiscovery_Correctly()
    {
        const string vcapApplication = """
            {
              "limits": {
                "fds": 16384,
                "mem": 512,
                "disk": 1024
              },
              "application_name": "foo",
              "application_uris": [
                "foo.apps.test-cloud.com"
              ],
              "name": "foo",
              "space_name": "test",
              "space_id": "98c627e7-f559-46a4-9032-88cab63f8249",
              "uris": [
                "foo.apps.test-cloud.com"
              ],
              "users": null,
              "version": "4a439db9-4a82-47a3-aeea-8240465cff8e",
              "application_version": "4a439db9-4a82-47a3-aeea-8240465cff8e",
              "application_id": "ac923014-93a5-4aee-b934-a043b241868b",
              "instance_id": "instance_id"
            }
            """;

        const string vcapServices = """
            {
              "p-config-server": [
                {
                  "credentials": {
                    "uri": "https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.test-cloud.com",
                    "client_id": "p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333",
                    "client_secret": "vBDjqIf7XthT",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                  },
                  "syslog_drain_url": null,
                  "label": "p-config-server",
                  "provider": null,
                  "plan": "standard",
                  "name": "myConfigServer",
                  "tags": [
                    "configuration",
                    "spring-cloud"
                  ]
                }
              ],
              "p-service-registry": [
                {
                  "credentials": {
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                  },
                  "syslog_drain_url": null,
                  "label": "p-service-registry",
                  "provider": null,
                  "plan": "standard",
                  "name": "myDiscoveryService",
                  "tags": [
                    "eureka",
                    "discovery",
                    "registry",
                    "spring-cloud"
                  ]
                }
              ]
            }
            """;

        const string appSettings = """
            {
              "spring": {
                "cloud": {
                  "discovery": {
                    "registrationMethod": "hostname"
                  }
                }
              },
              "eureka": {
                "client": {
                  "eurekaServer": {
                    "proxyHost": "proxyHost",
                    "proxyPort": 100,
                    "proxyUserName": "proxyUserName",
                    "proxyPassword": "proxyPassword",
                    "shouldGZipContent": true,
                    "connectTimeoutSeconds": 100
                  },
                  "allowRedirects": true,
                  "shouldDisableDelta": true,
                  "shouldFilterOnlyUpInstances": true,
                  "shouldFetchRegistry": false,
                  "registryRefreshSingleVipAddress": "registryRefreshSingleVipAddress",
                  "shouldRegisterWithEureka": false,
                  "registryFetchIntervalSeconds": 100,
                  "instanceInfoReplicationIntervalSeconds": 100,
                  "serviceUrl": "http://localhost:8761/eureka/"
                },
                "instance": {
                  "instanceId": "instanceId",
                  "appGroup": "appGroup",
                  "instanceEnabledOnInit": true,
                  "hostname": "my-hostname",
                  "port": 100,
                  "securePort": 100,
                  "nonSecurePortEnabled": true,
                  "securePortEnabled": true,
                  "leaseExpirationDurationInSeconds": 100,
                  "leaseRenewalIntervalInSeconds": 100,
                  "secureVipAddress": "secureVipAddress",
                  "vipAddress": "vipAddress",
                  "asgName": "asgName",
                  "metadataMap": {
                    "foo": "bar",
                    "bar": "foo"
                  },
                  "statusPageUrlPath": "statusPageUrlPath",
                  "statusPageUrl": "statusPageUrl",
                  "homePageUrlPath": "homePageUrlPath",
                  "homePageUrl": "homePageUrl",
                  "healthCheckUrlPath": "healthCheckUrlPath",
                  "healthCheckUrl": "healthCheckUrl",
                  "secureHealthCheckUrl": "secureHealthCheckUrl"
                }
              }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        await using var stream = TextConverter.ToStream(appSettings);
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        clientOptions.EurekaServer.ProxyHost.Should().Be("proxyHost");
        clientOptions.EurekaServer.ProxyPort.Should().Be(100);
        clientOptions.EurekaServer.ProxyPassword.Should().Be("proxyPassword");
        clientOptions.EurekaServer.ProxyUserName.Should().Be("proxyUserName");
        clientOptions.EurekaServer.ConnectTimeoutSeconds.Should().Be(100);
        clientOptions.EurekaServerServiceUrls.Should().Be("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com/eureka/");
        clientOptions.RegistryFetchIntervalSeconds.Should().Be(100);
        clientOptions.RegistryRefreshSingleVipAddress.Should().Be("registryRefreshSingleVipAddress");
        clientOptions.IsFetchDeltaDisabled.Should().BeTrue();
        clientOptions.ShouldFetchRegistry.Should().BeFalse();
        clientOptions.ShouldFilterOnlyUpInstances.Should().BeTrue();
        clientOptions.EurekaServer.ShouldGZipContent.Should().BeTrue();
        clientOptions.ShouldRegisterWithEureka.Should().BeFalse();
        clientOptions.AccessTokenUri.Should().Be("https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token");
        clientOptions.ClientId.Should().Be("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe");
        clientOptions.ClientSecret.Should().Be("dCsdoiuklicS");

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        instanceOptions.RegistrationMethod.Should().Be("hostname");
        instanceOptions.InstanceId.Should().Be("my-hostname:instance_id");
        instanceOptions.AppName.Should().Be("foo");
        instanceOptions.AppGroupName.Should().Be("appGroup");
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(100);
        instanceOptions.HostName.Should().Be("my-hostname");
        instanceOptions.SecurePort.Should().Be(100);
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(100);
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(100);
        instanceOptions.SecureVipAddress.Should().Be("secureVipAddress");
        instanceOptions.VipAddress.Should().Be("vipAddress");
        instanceOptions.AutoScalingGroupName.Should().Be("asgName");
        instanceOptions.StatusPageUrlPath.Should().Be("statusPageUrlPath");
        instanceOptions.StatusPageUrl.Should().Be("statusPageUrl");
        instanceOptions.HomePageUrlPath.Should().Be("homePageUrlPath");
        instanceOptions.HomePageUrl.Should().Be("homePageUrl");
        instanceOptions.HealthCheckUrlPath.Should().Be("healthCheckUrlPath");
        instanceOptions.HealthCheckUrl.Should().Be("healthCheckUrl");
        instanceOptions.SecureHealthCheckUrl.Should().Be("secureHealthCheckUrl");

        instanceOptions.MetadataMap.Should().HaveCount(6);
        instanceOptions.MetadataMap.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instanceOptions.MetadataMap.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        instanceOptions.MetadataMap.Should().ContainKey("instanceId").WhoseValue.Should().Be("instance_id");
        instanceOptions.MetadataMap.Should().ContainKey("cfAppGuid").WhoseValue.Should().Be("ac923014-93a5-4aee-b934-a043b241868b");
        instanceOptions.MetadataMap.Should().ContainKey("cfInstanceIndex").WhoseValue.Should().Be("1");
        instanceOptions.MetadataMap.Should().ContainKey("zone").WhoseValue.Should().Be("unknown");
    }

    [Fact]
    public async Task WithVCAPEnvVariables_Route_ConfiguresEurekaDiscovery_Correctly()
    {
        const string vcapApplication = """
            {
              "limits": {
                "fds": 16384,
                "mem": 512,
                "disk": 1024
              },
              "application_name": "foo",
              "application_uris": [
                "foo.apps.test-cloud.com"
              ],
              "name": "foo",
              "space_name": "test",
              "space_id": "98c627e7-f559-46a4-9032-88cab63f8249",
              "uris": [
                "foo.apps.test-cloud.com"
              ],
              "users": null,
              "version": "4a439db9-4a82-47a3-aeea-8240465cff8e",
              "application_version": "4a439db9-4a82-47a3-aeea-8240465cff8e",
              "application_id": "ac923014-93a5-4aee-b934-a043b241868b",
              "instance_id": "instance_id"
            }
            """;

        const string vcapServices = """
            {
              "p-config-server": [
                {
                  "credentials": {
                    "uri": "https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.test-cloud.com",
                    "client_id": "p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333",
                    "client_secret": "vBDjqIf7XthT",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                  },
                  "syslog_drain_url": null,
                  "label": "p-config-server",
                  "provider": null,
                  "plan": "standard",
                  "name": "myConfigServer",
                  "tags": [
                    "configuration",
                    "spring-cloud"
                  ]
                }
              ],
              "p-service-registry": [
                {
                  "credentials": {
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                  },
                  "syslog_drain_url": null,
                  "label": "p-service-registry",
                  "provider": null,
                  "plan": "standard",
                  "name": "myDiscoveryService",
                  "tags": [
                    "eureka",
                    "discovery",
                    "registry",
                    "spring-cloud"
                  ]
                }
              ]
            }
            """;

        const string appSettings = """
            {
              "eureka": {
                "client": {
                  "eurekaServer": {
                    "proxyHost": "proxyHost",
                    "proxyPort": 100,
                    "proxyUserName": "proxyUserName",
                    "proxyPassword": "proxyPassword",
                    "shouldGZipContent": true,
                    "connectTimeoutSeconds": 100
                  },
                  "allowRedirects": true,
                  "shouldDisableDelta": true,
                  "shouldFilterOnlyUpInstances": true,
                  "shouldFetchRegistry": false,
                  "registryRefreshSingleVipAddress": "registryRefreshSingleVipAddress",
                  "shouldRegisterWithEureka": false,
                  "registryFetchIntervalSeconds": 100,
                  "instanceInfoReplicationIntervalSeconds": 100,
                  "serviceUrl": "http://localhost:8761/eureka/"
                },
                "instance": {
                  "registrationMethod": "route",
                  "instanceId": "instanceId",
                  "appGroup": "appGroup",
                  "instanceEnabledOnInit": true,
                  "hostname": "my-hostname",
                  "port": 100,
                  "securePort": 100,
                  "nonSecurePortEnabled": true,
                  "securePortEnabled": true,
                  "leaseExpirationDurationInSeconds": 100,
                  "leaseRenewalIntervalInSeconds": 100,
                  "secureVipAddress": "secureVipAddress",
                  "vipAddress": "vipAddress",
                  "asgName": "asgName",
                  "metadataMap": {
                    "foo": "bar",
                    "bar": "foo"
                  },
                  "statusPageUrlPath": "statusPageUrlPath",
                  "statusPageUrl": "statusPageUrl",
                  "homePageUrlPath": "homePageUrlPath",
                  "homePageUrl": "homePageUrl",
                  "healthCheckUrlPath": "healthCheckUrlPath",
                  "healthCheckUrl": "healthCheckUrl",
                  "secureHealthCheckUrl": "secureHealthCheckUrl"
                }
              }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        await using var stream = TextConverter.ToStream(appSettings);
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        clientOptions.EurekaServer.ProxyHost.Should().Be("proxyHost");
        clientOptions.EurekaServer.ProxyPort.Should().Be(100);
        clientOptions.EurekaServer.ProxyPassword.Should().Be("proxyPassword");
        clientOptions.EurekaServer.ProxyUserName.Should().Be("proxyUserName");
        clientOptions.EurekaServer.ConnectTimeoutSeconds.Should().Be(100);
        clientOptions.EurekaServerServiceUrls.Should().Be("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com/eureka/");
        clientOptions.RegistryFetchIntervalSeconds.Should().Be(100);
        clientOptions.RegistryRefreshSingleVipAddress.Should().Be("registryRefreshSingleVipAddress");
        clientOptions.IsFetchDeltaDisabled.Should().BeTrue();
        clientOptions.ShouldFetchRegistry.Should().BeFalse();
        clientOptions.ShouldFilterOnlyUpInstances.Should().BeTrue();
        clientOptions.EurekaServer.ShouldGZipContent.Should().BeTrue();
        clientOptions.ShouldRegisterWithEureka.Should().BeFalse();
        clientOptions.AccessTokenUri.Should().Be("https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token");
        clientOptions.ClientId.Should().Be("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe");
        clientOptions.ClientSecret.Should().Be("dCsdoiuklicS");

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        instanceOptions.RegistrationMethod.Should().Be("route");
        instanceOptions.InstanceId.Should().Be("foo.apps.test-cloud.com:instance_id");
        instanceOptions.AppName.Should().Be("foo");
        instanceOptions.AppGroupName.Should().Be("appGroup");
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(80);
        instanceOptions.HostName.Should().Be("foo.apps.test-cloud.com");
        instanceOptions.SecurePort.Should().Be(443);
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(100);
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(100);
        instanceOptions.SecureVipAddress.Should().Be("secureVipAddress");
        instanceOptions.VipAddress.Should().Be("vipAddress");
        instanceOptions.AutoScalingGroupName.Should().Be("asgName");

        instanceOptions.StatusPageUrlPath.Should().Be("statusPageUrlPath");
        instanceOptions.StatusPageUrl.Should().Be("statusPageUrl");
        instanceOptions.HomePageUrlPath.Should().Be("homePageUrlPath");
        instanceOptions.HomePageUrl.Should().Be("homePageUrl");
        instanceOptions.HealthCheckUrlPath.Should().Be("healthCheckUrlPath");
        instanceOptions.HealthCheckUrl.Should().Be("healthCheckUrl");
        instanceOptions.SecureHealthCheckUrl.Should().Be("secureHealthCheckUrl");

        instanceOptions.MetadataMap.Should().HaveCount(6);
        instanceOptions.MetadataMap.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instanceOptions.MetadataMap.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        instanceOptions.MetadataMap.Should().ContainKey("instanceId").WhoseValue.Should().Be("instance_id");
        instanceOptions.MetadataMap.Should().ContainKey("cfAppGuid").WhoseValue.Should().Be("ac923014-93a5-4aee-b934-a043b241868b");
        instanceOptions.MetadataMap.Should().ContainKey("cfInstanceIndex").WhoseValue.Should().Be("1");
        instanceOptions.MetadataMap.Should().ContainKey("zone").WhoseValue.Should().Be("unknown");
    }

    [Fact]
    public async Task WithVCAPEnvVariables_AppName_Overrides_VCAPBinding()
    {
        const string vcapApplication = """
            {
              "limits": {
                "fds": 16384,
                "mem": 512,
                "disk": 1024
              },
              "application_name": "foo",
              "application_uris": [
                "foo.apps.test-cloud.com"
              ],
              "name": "foo",
              "space_name": "test",
              "space_id": "98c627e7-f559-46a4-9032-88cab63f8249",
              "uris": [
                "foo.apps.test-cloud.com"
              ],
              "users": null,
              "version": "4a439db9-4a82-47a3-aeea-8240465cff8e",
              "application_version": "4a439db9-4a82-47a3-aeea-8240465cff8e",
              "application_id": "ac923014-93a5-4aee-b934-a043b241868b",
              "instance_id": "instance_id"
            }
            """;

        const string vcapServices = """
            {
              "p-config-server": [
                {
                  "credentials": {
                    "uri": "https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.test-cloud.com",
                    "client_id": "p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333",
                    "client_secret": "vBDjqIf7XthT",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                  },
                  "syslog_drain_url": null,
                  "label": "p-config-server",
                  "provider": null,
                  "plan": "standard",
                  "name": "myConfigServer",
                  "tags": [
                    "configuration",
                    "spring-cloud"
                  ]
                }
              ],
              "p-service-registry": [
                {
                  "credentials": {
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token"
                  },
                  "syslog_drain_url": null,
                  "label": "p-service-registry",
                  "provider": null,
                  "plan": "standard",
                  "name": "myDiscoveryService",
                  "tags": [
                    "eureka",
                    "discovery",
                    "registry",
                    "spring-cloud"
                  ]
                }
              ]
            }
            """;

        const string appSettings = """
            {
              "eureka": {
                "client": {
                  "eurekaServer": {
                    "proxyHost": "proxyHost",
                    "proxyPort": 100,
                    "proxyUserName": "proxyUserName",
                    "proxyPassword": "proxyPassword",
                    "shouldGZipContent": true,
                    "connectTimeoutSeconds": 100
                  },
                  "allowRedirects": true,
                  "shouldDisableDelta": true,
                  "shouldFilterOnlyUpInstances": true,
                  "shouldFetchRegistry": false,
                  "registryRefreshSingleVipAddress": "registryRefreshSingleVipAddress",
                  "shouldRegisterWithEureka": false,
                  "registryFetchIntervalSeconds": 100,
                  "instanceInfoReplicationIntervalSeconds": 100,
                  "serviceUrl": "http://localhost:8761/eureka/"
                },
                "instance": {
                  "registrationMethod": "hostname",
                  "instanceId": "instanceId",
                  "appName": "appName",
                  "appGroup": "appGroup",
                  "instanceEnabledOnInit": true,
                  "hostname": "my-hostname",
                  "port": 100,
                  "securePort": 100,
                  "nonSecurePortEnabled": true,
                  "securePortEnabled": true,
                  "leaseExpirationDurationInSeconds": 100,
                  "leaseRenewalIntervalInSeconds": 100,
                  "secureVipAddress": "secureVipAddress",
                  "vipAddress": "vipAddress",
                  "asgName": "asgName",
                  "metadataMap": {
                    "foo": "bar",
                    "bar": "foo"
                  },
                  "statusPageUrlPath": "statusPageUrlPath",
                  "statusPageUrl": "statusPageUrl",
                  "homePageUrlPath": "homePageUrlPath",
                  "homePageUrl": "homePageUrl",
                  "healthCheckUrlPath": "healthCheckUrlPath",
                  "healthCheckUrl": "healthCheckUrl",
                  "secureHealthCheckUrl": "secureHealthCheckUrl"
                }
              }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        await using var stream = TextConverter.ToStream(appSettings);
        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        clientOptions.EurekaServer.ProxyHost.Should().Be("proxyHost");
        clientOptions.EurekaServer.ProxyPort.Should().Be(100);
        clientOptions.EurekaServer.ProxyPassword.Should().Be("proxyPassword");
        clientOptions.EurekaServer.ProxyUserName.Should().Be("proxyUserName");
        clientOptions.EurekaServer.ConnectTimeoutSeconds.Should().Be(100);
        clientOptions.EurekaServerServiceUrls.Should().Be("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.test-cloud.com/eureka/");
        clientOptions.RegistryFetchIntervalSeconds.Should().Be(100);
        clientOptions.RegistryRefreshSingleVipAddress.Should().Be("registryRefreshSingleVipAddress");
        clientOptions.IsFetchDeltaDisabled.Should().BeTrue();
        clientOptions.ShouldFetchRegistry.Should().BeFalse();
        clientOptions.ShouldFilterOnlyUpInstances.Should().BeTrue();
        clientOptions.EurekaServer.ShouldGZipContent.Should().BeTrue();
        clientOptions.ShouldRegisterWithEureka.Should().BeFalse();
        clientOptions.AccessTokenUri.Should().Be("https://p-spring-cloud-services.uaa.system.test-cloud.com/oauth/token");
        clientOptions.ClientId.Should().Be("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe");
        clientOptions.ClientSecret.Should().Be("dCsdoiuklicS");

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        instanceOptions.RegistrationMethod.Should().Be("hostname");
        instanceOptions.InstanceId.Should().Be("my-hostname:instance_id");
        instanceOptions.AppName.Should().Be("appName");
        instanceOptions.AppGroupName.Should().Be("appGroup");
        instanceOptions.IsInstanceEnabledOnInit.Should().BeTrue();
        instanceOptions.NonSecurePort.Should().Be(100);
        instanceOptions.HostName.Should().Be("my-hostname");
        instanceOptions.SecurePort.Should().Be(100);
        instanceOptions.IsNonSecurePortEnabled.Should().BeTrue();
        instanceOptions.IsSecurePortEnabled.Should().BeTrue();
        instanceOptions.LeaseExpirationDurationInSeconds.Should().Be(100);
        instanceOptions.LeaseRenewalIntervalInSeconds.Should().Be(100);
        instanceOptions.SecureVipAddress.Should().Be("secureVipAddress");
        instanceOptions.VipAddress.Should().Be("vipAddress");
        instanceOptions.AutoScalingGroupName.Should().Be("asgName");

        instanceOptions.StatusPageUrlPath.Should().Be("statusPageUrlPath");
        instanceOptions.StatusPageUrl.Should().Be("statusPageUrl");
        instanceOptions.HomePageUrlPath.Should().Be("homePageUrlPath");
        instanceOptions.HomePageUrl.Should().Be("homePageUrl");
        instanceOptions.HealthCheckUrlPath.Should().Be("healthCheckUrlPath");
        instanceOptions.HealthCheckUrl.Should().Be("healthCheckUrl");
        instanceOptions.SecureHealthCheckUrl.Should().Be("secureHealthCheckUrl");

        instanceOptions.MetadataMap.Should().HaveCount(6);
        instanceOptions.MetadataMap.Should().ContainKey("foo").WhoseValue.Should().Be("bar");
        instanceOptions.MetadataMap.Should().ContainKey("bar").WhoseValue.Should().Be("foo");
        instanceOptions.MetadataMap.Should().ContainKey("instanceId").WhoseValue.Should().Be("instance_id");
        instanceOptions.MetadataMap.Should().ContainKey("cfAppGuid").WhoseValue.Should().Be("ac923014-93a5-4aee-b934-a043b241868b");
        instanceOptions.MetadataMap.Should().ContainKey("cfInstanceIndex").WhoseValue.Should().Be("1");
        instanceOptions.MetadataMap.Should().ContainKey("zone").WhoseValue.Should().Be("unknown");
    }

    [Fact]
    public async Task WithVCAPEnvVariables_ButNoUri_DoesNotThrow()
    {
        const string vcapApplication = """
            {
              "application_name": "foo",
              "application_uris": [],
              "name": "foo",
              "uris": [],
              "application_id": "ac923014",
              "instance_id": "instance_id"
            }
            """;

        const string vcapServices = """
            {
              "p-service-registry": [
                {
                  "credentials": {
                    "uri": "https://eureka.apps.test-cloud.com"
                  },
                  "label": "p-service-registry",
                  "name": "myDiscoveryService",
                  "tags": [
                    "eureka"
                  ]
                }
              ]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        await using WebApplication app = builder.Build();

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

        Action action = () => _ = instanceOptionsMonitor.CurrentValue;

        action.Should().NotThrow("Binding Eureka instance configuration should not throw for no Uri.");
    }
}
