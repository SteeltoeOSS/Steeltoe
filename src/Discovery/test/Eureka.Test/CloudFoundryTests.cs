// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class CloudFoundryTests
{
    [Fact]
    public void NoVCAPEnvVariables_ConfiguresEurekaDiscovery_Correctly()
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

        using Stream stream = TestHelpers.StringToStream(appSettings);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("http://api.eureka.com:8761/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.IsFetchDeltaDisabled);
        Assert.False(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.False(clientOptions.ShouldRegisterWithEureka);

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        Assert.Equal("instanceId", instanceOptions.InstanceId);
        Assert.Equal("appName", instanceOptions.AppName);
        Assert.Equal("appGroup", instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(100, instanceOptions.NonSecurePort);
        Assert.Equal("hostname", instanceOptions.HostName);
        Assert.Equal(100, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(100, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Equal(100, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal("secureVipAddress", instanceOptions.SecureVipAddress);
        Assert.Equal("vipAddress", instanceOptions.VipAddress);
        Assert.Equal("asgName", instanceOptions.AutoScalingGroupName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        Assert.Equal(2, instanceOptions.MetadataMap.Count);
        Assert.Equal("bar", instanceOptions.MetadataMap["foo"]);
        Assert.Equal("foo", instanceOptions.MetadataMap["bar"]);
    }

    [Fact]
    public void WithVCAPEnvVariables_HostName_ConfiguresEurekaDiscovery_Correctly()
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
                "foo.apps.testcloud.com"
              ],
              "name": "foo",
              "space_name": "test",
              "space_id": "98c627e7-f559-46a4-9032-88cab63f8249",
              "uris": [
                "foo.apps.testcloud.com"
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
                    "uri": "https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.testcloud.com",
                    "client_id": "p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333",
                    "client_secret": "vBDjqIf7XthT",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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
                  "hostname": "myhostname",
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

        using Stream stream = TestHelpers.StringToStream(appSettings);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.IsFetchDeltaDisabled);
        Assert.False(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.False(clientOptions.ShouldRegisterWithEureka);
        Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", clientOptions.AccessTokenUri);
        Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", clientOptions.ClientId);
        Assert.Equal("dCsdoiuklicS", clientOptions.ClientSecret);

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        Assert.Equal("hostname", instanceOptions.RegistrationMethod);
        Assert.Equal("myhostname:instance_id", instanceOptions.InstanceId);
        Assert.Equal("foo", instanceOptions.AppName);
        Assert.Equal("appGroup", instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(100, instanceOptions.NonSecurePort);
        Assert.Equal("myhostname", instanceOptions.HostName);
        Assert.Equal(100, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(100, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Equal(100, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal("secureVipAddress", instanceOptions.SecureVipAddress);
        Assert.Equal("vipAddress", instanceOptions.VipAddress);
        Assert.Equal("asgName", instanceOptions.AutoScalingGroupName);
        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        Assert.Equal(6, instanceOptions.MetadataMap.Count);
        Assert.Equal("bar", instanceOptions.MetadataMap["foo"]);
        Assert.Equal("foo", instanceOptions.MetadataMap["bar"]);
        Assert.Equal("instance_id", instanceOptions.MetadataMap["instanceId"]);
        Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", instanceOptions.MetadataMap["cfAppGuid"]);
        Assert.Equal("1", instanceOptions.MetadataMap["cfInstanceIndex"]);
        Assert.Equal("unknown", instanceOptions.MetadataMap["zone"]);
    }

    [Fact]
    public void WithVCAPEnvVariables_Route_ConfiguresEurekaDiscovery_Correctly()
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
                "foo.apps.testcloud.com"
              ],
              "name": "foo",
              "space_name": "test",
              "space_id": "98c627e7-f559-46a4-9032-88cab63f8249",
              "uris": [
                "foo.apps.testcloud.com"
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
                    "uri": "https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.testcloud.com",
                    "client_id": "p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333",
                    "client_secret": "vBDjqIf7XthT",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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
                  "hostname": "myhostname",
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

        using Stream stream = TestHelpers.StringToStream(appSettings);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.IsFetchDeltaDisabled);
        Assert.False(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.False(clientOptions.ShouldRegisterWithEureka);
        Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", clientOptions.AccessTokenUri);
        Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", clientOptions.ClientId);
        Assert.Equal("dCsdoiuklicS", clientOptions.ClientSecret);

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        Assert.Equal("route", instanceOptions.RegistrationMethod);
        Assert.Equal("foo.apps.testcloud.com:instance_id", instanceOptions.InstanceId);
        Assert.Equal("foo", instanceOptions.AppName);
        Assert.Equal("appGroup", instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(80, instanceOptions.NonSecurePort);
        Assert.Equal("foo.apps.testcloud.com", instanceOptions.HostName);
        Assert.Equal(443, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(100, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Equal(100, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal("secureVipAddress", instanceOptions.SecureVipAddress);
        Assert.Equal("vipAddress", instanceOptions.VipAddress);
        Assert.Equal("asgName", instanceOptions.AutoScalingGroupName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        Assert.Equal(6, instanceOptions.MetadataMap.Count);
        Assert.Equal("bar", instanceOptions.MetadataMap["foo"]);
        Assert.Equal("foo", instanceOptions.MetadataMap["bar"]);
        Assert.Equal("instance_id", instanceOptions.MetadataMap["instanceId"]);
        Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", instanceOptions.MetadataMap["cfAppGuid"]);
        Assert.Equal("1", instanceOptions.MetadataMap["cfInstanceIndex"]);
        Assert.Equal("unknown", instanceOptions.MetadataMap["zone"]);
    }

    [Fact]
    public void WithVCAPEnvVariables_AppName_Overrides_VCAPBinding()
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
                "foo.apps.testcloud.com"
              ],
              "name": "foo",
              "space_name": "test",
              "space_id": "98c627e7-f559-46a4-9032-88cab63f8249",
              "uris": [
                "foo.apps.testcloud.com"
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
                    "uri": "https://config-de211817-2e99-4c57-89e8-31fa7ca6a276.apps.testcloud.com",
                    "client_id": "p-config-server-8f49dd26-e6cd-47a6-b2a0-7655cea20333",
                    "client_secret": "vBDjqIf7XthT",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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
                    "uri": "https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com",
                    "client_id": "p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe",
                    "client_secret": "dCsdoiuklicS",
                    "access_token_uri": "https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token"
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
                  "hostname": "myhostname",
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

        using Stream stream = TestHelpers.StringToStream(appSettings);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.Configuration.AddJsonStream(stream);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        using WebApplication app = builder.Build();

        var clientOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaClientOptions>>();
        EurekaClientOptions clientOptions = clientOptionsMonitor.CurrentValue;

        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.IsFetchDeltaDisabled);
        Assert.False(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.False(clientOptions.ShouldRegisterWithEureka);
        Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", clientOptions.AccessTokenUri);
        Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", clientOptions.ClientId);
        Assert.Equal("dCsdoiuklicS", clientOptions.ClientSecret);

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = instanceOptionsMonitor.CurrentValue;

        Assert.Equal("hostname", instanceOptions.RegistrationMethod);
        Assert.Equal("myhostname:instance_id", instanceOptions.InstanceId);
        Assert.Equal("appName", instanceOptions.AppName);
        Assert.Equal("appGroup", instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(100, instanceOptions.NonSecurePort);
        Assert.Equal("myhostname", instanceOptions.HostName);
        Assert.Equal(100, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(100, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Equal(100, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal("secureVipAddress", instanceOptions.SecureVipAddress);
        Assert.Equal("vipAddress", instanceOptions.VipAddress);
        Assert.Equal("asgName", instanceOptions.AutoScalingGroupName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        Assert.Equal(6, instanceOptions.MetadataMap.Count);
        Assert.Equal("bar", instanceOptions.MetadataMap["foo"]);
        Assert.Equal("foo", instanceOptions.MetadataMap["bar"]);
        Assert.Equal("instance_id", instanceOptions.MetadataMap["instanceId"]);
        Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", instanceOptions.MetadataMap["cfAppGuid"]);
        Assert.Equal("1", instanceOptions.MetadataMap["cfInstanceIndex"]);
        Assert.Equal("unknown", instanceOptions.MetadataMap["zone"]);
    }

    [Fact]
    public void WithVCAPEnvVariables_ButNoUri_DoesNotThrow()
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
                    "uri": "https://eureka.apps.testcloud.com"
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

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseDefaultServiceProvider(options => options.ValidateScopes = true);
        builder.AddCloudFoundryConfiguration();
        builder.Configuration.AddCloudFoundryServiceBindings();
        builder.Services.AddEurekaDiscoveryClient();

        using WebApplication app = builder.Build();

        var instanceOptionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();

        Action action = () => _ = instanceOptionsMonitor.CurrentValue;

        action.Should().NotThrow("Binding Eureka instance configuration should not throw for no Uri.");
    }
}
