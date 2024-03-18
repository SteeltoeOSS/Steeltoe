// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.CloudFoundry;
using Steeltoe.Connectors.Services;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaPostConfigurerTest
{
    [Fact]
    public void UpdateConfiguration_WithInstDefaults_UpdatesCorrectly()
    {
        var builder = new ConfigurationBuilder();

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "bar" },
            { "spring:application:instance_id", "instance" },
            { "spring:cloud:discovery:registrationMethod", "registrationMethod" }
        });

        IConfigurationRoot root = builder.Build();

        var instanceOptions = new EurekaInstanceOptions();
        EurekaPostConfigurer.UpdateConfiguration(root, instanceOptions, new ApplicationInstanceInfo(root));

        Assert.Equal("bar", instanceOptions.AppName);
        Assert.Equal("instance", instanceOptions.InstanceId);
        Assert.Equal("registrationMethod", instanceOptions.RegistrationMethod);
        Assert.Equal("bar", instanceOptions.VirtualHostName);
        Assert.Equal("bar", instanceOptions.SecureVirtualHostName);
    }

    [Fact]
    public void UpdateConfiguration_UpdatesCorrectly()
    {
        var builder = new ConfigurationBuilder();

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "bar" },
            { "spring:application:instance_id", "instance" },
            { "spring:cloud:discovery:registrationMethod", "registrationMethod" }
        });

        IConfigurationRoot root = builder.Build();

        var instanceOptions = new EurekaInstanceOptions
        {
            AppName = "doNotChange",
            InstanceId = "doNotChange",
            RegistrationMethod = "doNotChange"
        };

        EurekaPostConfigurer.UpdateConfiguration(root, instanceOptions, null);

        Assert.Equal("doNotChange", instanceOptions.AppName);
        Assert.Equal("doNotChange", instanceOptions.InstanceId);
        Assert.Equal("doNotChange", instanceOptions.RegistrationMethod);
        Assert.Equal("doNotChange", instanceOptions.VirtualHostName);
        Assert.Equal("doNotChange", instanceOptions.SecureVirtualHostName);
    }

    [Fact]
    public void UpdateConfiguration_UpdatesCorrectly2()
    {
        var builder = new ConfigurationBuilder();

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:name", "bar" },
            { "spring:cloud:discovery:registrationMethod", "registrationMethod" }
        });

        IConfigurationRoot root = builder.Build();

        var instanceOptions = new EurekaInstanceOptions();

        EurekaPostConfigurer.UpdateConfiguration(root, instanceOptions, new ApplicationInstanceInfo(root));

        Assert.Equal("bar", instanceOptions.AppName);
        Assert.EndsWith("bar:80", instanceOptions.InstanceId, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("registrationMethod", instanceOptions.RegistrationMethod);
        Assert.Equal("bar", instanceOptions.VirtualHostName);
        Assert.Equal("bar", instanceOptions.SecureVirtualHostName);
    }

    [Fact]
    public void UpdateConfiguration_UpdatesCorrectly3()
    {
        var builder = new ConfigurationBuilder();

        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "spring:application:instance_id", "instance" },
            { "spring:cloud:discovery:registrationMethod", "registrationMethod" }
        });

        IConfigurationRoot root = builder.Build();

        var instanceOptions = new EurekaInstanceOptions
        {
            AppName = "doNotChange",
            InstanceId = "doNotChange",
            RegistrationMethod = "doNotChange"
        };

        EurekaPostConfigurer.UpdateConfiguration(root, instanceOptions, null);

        Assert.Equal("doNotChange", instanceOptions.AppName);
        Assert.Equal("doNotChange", instanceOptions.InstanceId);
        Assert.Equal("doNotChange", instanceOptions.RegistrationMethod);
        Assert.Equal("doNotChange", instanceOptions.VirtualHostName);
        Assert.Equal("doNotChange", instanceOptions.SecureVirtualHostName);
    }

    [Fact]
    public void UpdateConfiguration_NoServiceInfo_ConfiguresEurekaDiscovery_Correctly()
    {
        const string appsettings = """
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
                        "shouldFetchRegistry": true,
                        "registryRefreshSingleVipAddress":"registryRefreshSingleVipAddress",
                        "shouldOnDemandUpdateStatusChange": true,
                        "shouldRegisterWithEureka": true,
                        "registryFetchIntervalSeconds": 100,
                        "instanceInfoReplicationIntervalSeconds": 100,
                        "serviceUrl": "http://localhost:8761/eureka/"
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
                        "leaseExpirationDurationInSeconds":100,
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
                        "homePageUrlPath":"homePageUrlPath",
                        "homePageUrl": "homePageUrl",
                        "healthCheckUrlPath": "healthCheckUrlPath",
                        "healthCheckUrl":"healthCheckUrl",
                        "secureHealthCheckUrl":"secureHealthCheckUrl"
                    }
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(sandbox.FullPath);
        configurationBuilder.AddJsonFile(Path.GetFileName(path));
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        var clientOptions = new EurekaClientOptions();
        IConfigurationSection clientSection = configurationRoot.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);
        clientSection.Bind(clientOptions);

        EurekaPostConfigurer.UpdateConfiguration(null, clientOptions);

        Assert.NotNull(clientOptions);
        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("http://localhost:8761/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
        Assert.True(clientOptions.ShouldRegisterWithEureka);

        var instanceOptions = new EurekaInstanceOptions();
        IConfigurationSection instanceSection = configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
        instanceSection.Bind(instanceOptions);

        EurekaPostConfigurer.UpdateConfiguration(configurationRoot, null, instanceOptions, null);

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
        Assert.Equal("secureVipAddress", instanceOptions.SecureVirtualHostName);
        Assert.Equal("vipAddress", instanceOptions.VirtualHostName);
        Assert.Equal("asgName", instanceOptions.AsgName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        IDictionary<string, string> map = instanceOptions.MetadataMap;
        Assert.NotNull(map);
        Assert.Equal(2, map.Count);
        Assert.Equal("bar", map["foo"]);
        Assert.Equal("foo", map["bar"]);
    }

    [Fact]
    public void UpdateConfigurationComplainsAboutDefaultWhenWontWork()
    {
        using var scope = new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true");

        var exception = Assert.Throws<InvalidOperationException>(() => EurekaPostConfigurer.UpdateConfiguration(null, new EurekaClientOptions()));
        Assert.Contains(EurekaClientOptions.DefaultServerServiceUrl, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateConfiguration_WithVCAPEnvVariables_HostName_ConfiguresEurekaDiscovery_Correctly()
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
                "p-config-server": [{
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
                }],
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
                }]
            }
            """;

        const string appsettings = """
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
                        "shouldFetchRegistry": true,
                        "registryRefreshSingleVipAddress":"registryRefreshSingleVipAddress",
                        "shouldOnDemandUpdateStatusChange": true,
                        "shouldRegisterWithEureka": true,
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
                        "leaseExpirationDurationInSeconds":100,
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
                        "homePageUrlPath":"homePageUrlPath",
                        "homePageUrl": "homePageUrl",
                        "healthCheckUrlPath": "healthCheckUrlPath",
                        "healthCheckUrl":"healthCheckUrl",
                        "secureHealthCheckUrl":"secureHealthCheckUrl"
                    }
                }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IEnumerable<EurekaServiceInfo> sis = configurationRoot.GetServiceInfos<EurekaServiceInfo>();
        Assert.Single(sis);
        EurekaServiceInfo si = sis.First();

        var clientOptions = new EurekaClientOptions();
        IConfigurationSection clientSection = configurationRoot.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);
        clientSection.Bind(clientOptions);

        EurekaPostConfigurer.UpdateConfiguration(si, clientOptions);

        Assert.NotNull(clientOptions);
        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
        Assert.True(clientOptions.ShouldRegisterWithEureka);
        Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", clientOptions.AccessTokenUri);
        Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", clientOptions.ClientId);
        Assert.Equal("dCsdoiuklicS", clientOptions.ClientSecret);

        var instanceOptions = new EurekaInstanceOptions();
        IConfigurationSection instanceSection = configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
        instanceSection.Bind(instanceOptions);

        EurekaPostConfigurer.UpdateConfiguration(configurationRoot, si, instanceOptions, si.ApplicationInfo);

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
        Assert.Equal("secureVipAddress", instanceOptions.SecureVirtualHostName);
        Assert.Equal("vipAddress", instanceOptions.VirtualHostName);
        Assert.Equal("asgName", instanceOptions.AsgName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        IDictionary<string, string> map = instanceOptions.MetadataMap;
        Assert.NotNull(map);
        Assert.Equal(6, map.Count);
        Assert.Equal("bar", map["foo"]);
        Assert.Equal("foo", map["bar"]);
        Assert.Equal("instance_id", map[EurekaPostConfigurer.InstanceId]);
        Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", map[EurekaPostConfigurer.CFAppGuid]);
        Assert.Equal("1", map[EurekaPostConfigurer.CFInstanceIndex]);
        Assert.Equal(EurekaPostConfigurer.UnknownZone, map[EurekaPostConfigurer.Zone]);
    }

    [Fact]
    public void UpdateConfiguration_WithVCAPEnvVariables_Route_ConfiguresEurekaDiscovery_Correctly()
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
                "p-config-server": [{
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
                }],
                "p-service-registry": [{
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
                }]
            }
            """;

        const string appsettings = """
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
                        "shouldFetchRegistry": true,
                        "registryRefreshSingleVipAddress":"registryRefreshSingleVipAddress",
                        "shouldOnDemandUpdateStatusChange": true,
                        "shouldRegisterWithEureka": true,
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
                        "leaseExpirationDurationInSeconds":100,
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
                        "homePageUrlPath":"homePageUrlPath",
                        "homePageUrl": "homePageUrl",
                        "healthCheckUrlPath": "healthCheckUrlPath",
                        "healthCheckUrl":"healthCheckUrl",
                        "secureHealthCheckUrl":"secureHealthCheckUrl"
                    }
                }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IEnumerable<EurekaServiceInfo> sis = configurationRoot.GetServiceInfos<EurekaServiceInfo>();
        Assert.Single(sis);
        EurekaServiceInfo si = sis.First();

        var clientOptions = new EurekaClientOptions();
        IConfigurationSection clientSection = configurationRoot.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);
        clientSection.Bind(clientOptions);

        EurekaPostConfigurer.UpdateConfiguration(si, clientOptions);

        Assert.NotNull(clientOptions);
        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
        Assert.True(clientOptions.ShouldRegisterWithEureka);
        Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", clientOptions.AccessTokenUri);
        Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", clientOptions.ClientId);
        Assert.Equal("dCsdoiuklicS", clientOptions.ClientSecret);

        var instanceOptions = new EurekaInstanceOptions();
        IConfigurationSection instanceSection = configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
        instanceSection.Bind(instanceOptions);

        EurekaPostConfigurer.UpdateConfiguration(configurationRoot, si, instanceOptions, si.ApplicationInfo);

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
        Assert.Equal("secureVipAddress", instanceOptions.SecureVirtualHostName);
        Assert.Equal("vipAddress", instanceOptions.VirtualHostName);
        Assert.Equal("asgName", instanceOptions.AsgName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        IDictionary<string, string> map = instanceOptions.MetadataMap;
        Assert.NotNull(map);
        Assert.Equal(6, map.Count);
        Assert.Equal("bar", map["foo"]);
        Assert.Equal("foo", map["bar"]);
        Assert.Equal("instance_id", map[EurekaPostConfigurer.InstanceId]);
        Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", map[EurekaPostConfigurer.CFAppGuid]);
        Assert.Equal("1", map[EurekaPostConfigurer.CFInstanceIndex]);
        Assert.Equal(EurekaPostConfigurer.UnknownZone, map[EurekaPostConfigurer.Zone]);
    }

    [Fact]
    public void UpdateConfiguration_WithVCAPEnvVariables_AppName_Overrides_VCAPBinding()
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
                "p-config-server": [{
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
                }],
                "p-service-registry": [{
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
                }]
            }
            """;

        const string appsettings = """
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
                        "shouldFetchRegistry": true,
                        "registryRefreshSingleVipAddress":"registryRefreshSingleVipAddress",
                        "shouldOnDemandUpdateStatusChange": true,
                        "shouldRegisterWithEureka": true,
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
                        "leaseExpirationDurationInSeconds":100,
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
                        "homePageUrlPath":"homePageUrlPath",
                        "homePageUrl": "homePageUrl",
                        "healthCheckUrlPath": "healthCheckUrlPath",
                        "healthCheckUrl":"healthCheckUrl",
                        "secureHealthCheckUrl":"secureHealthCheckUrl"
                    }
                }
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        configurationBuilder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IEnumerable<EurekaServiceInfo> sis = configurationRoot.GetServiceInfos<EurekaServiceInfo>();
        Assert.Single(sis);
        EurekaServiceInfo si = sis.First();

        var clientOptions = new EurekaClientOptions();
        IConfigurationSection clientSection = configurationRoot.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);
        clientSection.Bind(clientOptions);

        EurekaPostConfigurer.UpdateConfiguration(si, clientOptions);

        Assert.NotNull(clientOptions);
        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://eureka-6a1b81f5-79e2-4d14-a86b-ddf584635a60.apps.testcloud.com/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
        Assert.True(clientOptions.ShouldRegisterWithEureka);
        Assert.Equal("https://p-spring-cloud-services.uaa.system.testcloud.com/oauth/token", clientOptions.AccessTokenUri);
        Assert.Equal("p-service-registry-06e28efd-24be-4ce3-9784-854ed8d2acbe", clientOptions.ClientId);
        Assert.Equal("dCsdoiuklicS", clientOptions.ClientSecret);

        var instanceOptions = new EurekaInstanceOptions();
        IConfigurationSection instanceSection = configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
        instanceSection.Bind(instanceOptions);

        EurekaPostConfigurer.UpdateConfiguration(configurationRoot, si, instanceOptions, si.ApplicationInfo);

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
        Assert.Equal("secureVipAddress", instanceOptions.SecureVirtualHostName);
        Assert.Equal("vipAddress", instanceOptions.VirtualHostName);
        Assert.Equal("asgName", instanceOptions.AsgName);

        Assert.Equal("statusPageUrlPath", instanceOptions.StatusPageUrlPath);
        Assert.Equal("statusPageUrl", instanceOptions.StatusPageUrl);
        Assert.Equal("homePageUrlPath", instanceOptions.HomePageUrlPath);
        Assert.Equal("homePageUrl", instanceOptions.HomePageUrl);
        Assert.Equal("healthCheckUrlPath", instanceOptions.HealthCheckUrlPath);
        Assert.Equal("healthCheckUrl", instanceOptions.HealthCheckUrl);
        Assert.Equal("secureHealthCheckUrl", instanceOptions.SecureHealthCheckUrl);

        IDictionary<string, string> map = instanceOptions.MetadataMap;
        Assert.NotNull(map);
        Assert.Equal(6, map.Count);
        Assert.Equal("bar", map["foo"]);
        Assert.Equal("foo", map["bar"]);
        Assert.Equal("instance_id", map[EurekaPostConfigurer.InstanceId]);
        Assert.Equal("ac923014-93a5-4aee-b934-a043b241868b", map[EurekaPostConfigurer.CFAppGuid]);
        Assert.Equal("1", map[EurekaPostConfigurer.CFInstanceIndex]);
        Assert.Equal(EurekaPostConfigurer.UnknownZone, map[EurekaPostConfigurer.Zone]);
    }

    [Fact]
    public void UpdateConfiguration_WithVCAPEnvVariables_ButNoUri_DoesNotThrow()
    {
        const string vcapApplication = """
            {
                "application_name": "foo",
                "application_uris": [ ],
                "name": "foo",
                "uris": [ ],
                "application_id": "ac923014",
                "instance_id": "instance_id"
            }
            """;

        const string vcapServices = """
            {
                "p-service-registry": [{
                    "credentials": {
                        "uri": "https://eureka.apps.testcloud.com",
                    },
                    "label": "p-service-registry",
                    "name": "myDiscoveryService",
                    "tags": [
                        "eureka"
                    ]
                }]
            }
            """;

        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", vcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", vcapServices);
        using var indexScope = new EnvironmentVariableScope("CF_INSTANCE_INDEX", "1");
        using var guidScope = new EnvironmentVariableScope("CF_INSTANCE_GUID", "ac923014-93a5-4aee-b934-a043b241868b");

        using var sandbox = new Sandbox();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddCloudFoundry().Build();
        EurekaServiceInfo si = configurationRoot.GetServiceInfos<EurekaServiceInfo>().First();

        var clientOptions = new EurekaClientOptions();
        EurekaPostConfigurer.UpdateConfiguration(si, clientOptions);

        var instanceOptions = new EurekaInstanceOptions();
        IConfigurationSection instanceOptionsSection = configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
        instanceOptionsSection.Bind(instanceOptions);

        void ConfigureAction()
        {
            EurekaPostConfigurer.UpdateConfiguration(configurationRoot, si, instanceOptions, si.ApplicationInfo);
        }

        Action configureAction = ConfigureAction;

        configureAction.Should().NotThrow("UpdateConfiguration should not throw for no Uri");
    }

    [Fact]
    public void UpdateConfigurationFindsUrls()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" }
        }).Build();

        var instanceOptions = new EurekaInstanceOptions();
        var appInfo = new ApplicationInstanceInfo(configurationRoot);

        EurekaPostConfigurer.UpdateConfiguration(configurationRoot, instanceOptions, appInfo);

        Assert.Equal("myapp", instanceOptions.HostName);
        Assert.Equal(1234, instanceOptions.SecurePort);
        Assert.Equal(1233, instanceOptions.NonSecurePort);
    }

    [Fact]
    public void UpdateConfiguration_DisableClientShouldNotComplainAboutInvalidConfiguration()
    {
        var clientOptions = new EurekaClientOptions
        {
            Enabled = false
        };

        using var scope = new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true");

        Exception ex = Record.Exception(() => EurekaPostConfigurer.UpdateConfiguration(null, clientOptions));
        Assert.Null(ex);
    }
}
