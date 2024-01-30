// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Utils.IO;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaClientOptionsTest : AbstractBaseTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var clientOptions = new EurekaClientOptions();

        Assert.Equal(EurekaClientOptions.DefaultRegistryFetchIntervalSeconds, clientOptions.RegistryFetchIntervalSeconds);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.Equal(EurekaServerConfiguration.DefaultConnectTimeoutSeconds, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.True(clientOptions.ShouldRegisterWithEureka);
        Assert.False(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
    }

    [Fact]
    public void Constructor_Initializes_Defaults()
    {
        var clientOptions = new EurekaClientOptions();

        Assert.True(clientOptions.Enabled);
        Assert.Equal(EurekaClientOptions.DefaultRegistryFetchIntervalSeconds, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Null(clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(0, clientOptions.EurekaServer.ProxyPort);
        Assert.Null(clientOptions.EurekaServer.ProxyUserName);
        Assert.Null(clientOptions.EurekaServer.ProxyPassword);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.Equal(EurekaServerConfiguration.DefaultConnectTimeoutSeconds, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.True(clientOptions.ShouldRegisterWithEureka);
        Assert.False(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.Null(clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
        Assert.Equal(EurekaClientOptions.DefaultServerServiceUrl, clientOptions.EurekaServerServiceUrls);
        Assert.NotNull(clientOptions.Health);
        Assert.True(clientOptions.Health.Enabled); // Health contributor enabled
        Assert.True(clientOptions.Health.CheckEnabled); // Health check enabled
        Assert.Null(clientOptions.Health.MonitoredApps);
    }

    [Fact]
    public void Constructor_ConfiguresEurekaDiscovery_Correctly()
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
                        "serviceUrl": "https://foo.bar:8761/eureka/"
                    },
                    "instance": {
                        "registrationMethod" : "foobar",
                        "hostName": "myHostName",
                        "instanceId": "instanceId",
                        "appName": "appName",
                        "appGroup": "appGroup",
                        "instanceEnabledOnInit": true,
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
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IConfigurationSection clientSection = configurationRoot.GetSection(EurekaClientOptions.EurekaClientConfigurationPrefix);
        var clientOptions = new EurekaClientOptions();
        clientSection.Bind(clientOptions);

        Assert.Equal("proxyHost", clientOptions.EurekaServer.ProxyHost);
        Assert.Equal(100, clientOptions.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", clientOptions.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", clientOptions.EurekaServer.ProxyUserName);
        Assert.Equal(100, clientOptions.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://foo.bar:8761/eureka/", clientOptions.EurekaServerServiceUrls);
        Assert.Equal(100, clientOptions.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", clientOptions.RegistryRefreshSingleVipAddress);
        Assert.True(clientOptions.ShouldDisableDelta);
        Assert.True(clientOptions.ShouldFetchRegistry);
        Assert.True(clientOptions.ShouldFilterOnlyUpInstances);
        Assert.True(clientOptions.EurekaServer.ShouldGZipContent);
        Assert.True(clientOptions.ShouldOnDemandUpdateStatusChange);
        Assert.True(clientOptions.ShouldRegisterWithEureka);
        Assert.NotNull(clientOptions.Health);
        Assert.True(clientOptions.Health.Enabled); // Health contributor enabled
        Assert.True(clientOptions.Health.CheckEnabled); // Health check enabled
        Assert.Null(clientOptions.Health.MonitoredApps);
    }
}
