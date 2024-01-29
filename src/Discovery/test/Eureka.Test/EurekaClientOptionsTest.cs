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
        var options = new EurekaClientOptions();

        Assert.Equal(EurekaClientOptions.DefaultRegistryFetchIntervalSeconds, options.RegistryFetchIntervalSeconds);
        Assert.True(options.EurekaServer.ShouldGZipContent);
        Assert.Equal(EurekaServerConfiguration.DefaultConnectTimeoutSeconds, options.EurekaServer.ConnectTimeoutSeconds);
        Assert.True(options.ShouldRegisterWithEureka);
        Assert.False(options.ShouldDisableDelta);
        Assert.True(options.ShouldFilterOnlyUpInstances);
        Assert.True(options.ShouldFetchRegistry);
        Assert.True(options.ShouldOnDemandUpdateStatusChange);
    }

    [Fact]
    public void Constructor_Initializes_Defaults()
    {
        var options = new EurekaClientOptions();

        Assert.True(options.Enabled);
        Assert.Equal(EurekaClientOptions.DefaultRegistryFetchIntervalSeconds, options.RegistryFetchIntervalSeconds);
        Assert.Null(options.EurekaServer.ProxyHost);
        Assert.Equal(0, options.EurekaServer.ProxyPort);
        Assert.Null(options.EurekaServer.ProxyUserName);
        Assert.Null(options.EurekaServer.ProxyPassword);
        Assert.True(options.EurekaServer.ShouldGZipContent);
        Assert.Equal(EurekaServerConfiguration.DefaultConnectTimeoutSeconds, options.EurekaServer.ConnectTimeoutSeconds);
        Assert.True(options.ShouldRegisterWithEureka);
        Assert.False(options.ShouldDisableDelta);
        Assert.True(options.ShouldFilterOnlyUpInstances);
        Assert.True(options.ShouldFetchRegistry);
        Assert.Null(options.RegistryRefreshSingleVipAddress);
        Assert.True(options.ShouldOnDemandUpdateStatusChange);
        Assert.Equal(EurekaClientOptions.DefaultServerServiceUrl, options.EurekaServerServiceUrls);
        Assert.NotNull(options.Health);
        Assert.True(options.Health.Enabled); // Health contrib enabled
        Assert.True(options.Health.CheckEnabled); // Health check enabled
        Assert.Null(options.Health.MonitoredApps);
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
        var options = new EurekaClientOptions();
        clientSection.Bind(options);

        Assert.Equal("proxyHost", options.EurekaServer.ProxyHost);
        Assert.Equal(100, options.EurekaServer.ProxyPort);
        Assert.Equal("proxyPassword", options.EurekaServer.ProxyPassword);
        Assert.Equal("proxyUserName", options.EurekaServer.ProxyUserName);
        Assert.Equal(100, options.EurekaServer.ConnectTimeoutSeconds);
        Assert.Equal("https://foo.bar:8761/eureka/", options.EurekaServerServiceUrls);
        Assert.Equal(100, options.RegistryFetchIntervalSeconds);
        Assert.Equal("registryRefreshSingleVipAddress", options.RegistryRefreshSingleVipAddress);
        Assert.True(options.ShouldDisableDelta);
        Assert.True(options.ShouldFetchRegistry);
        Assert.True(options.ShouldFilterOnlyUpInstances);
        Assert.True(options.EurekaServer.ShouldGZipContent);
        Assert.True(options.ShouldOnDemandUpdateStatusChange);
        Assert.True(options.ShouldRegisterWithEureka);
        Assert.NotNull(options.Health);
        Assert.True(options.Health.Enabled); // Health contrib enabled
        Assert.True(options.Health.CheckEnabled); // Health check enabled
        Assert.Null(options.Health.MonitoredApps);
    }
}
