// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaInstanceOptionsTest : AbstractBaseTest
{
    [Fact]
    public void DefaultConstructor_InitializedWithDefaults()
    {
        var instanceOptions = new EurekaInstanceOptions();

        string thisHostAddress = instanceOptions.GetHostAddress(false);

        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(EurekaInstanceOptions.DefaultNonSecurePort, instanceOptions.NonSecurePort);
        Assert.Equal(EurekaInstanceOptions.DefaultSecurePort, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.False(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(EurekaInstanceOptions.DefaultLeaseRenewalIntervalInSeconds, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal(EurekaInstanceOptions.DefaultLeaseExpirationDurationInSeconds, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Null(instanceOptions.SecureVirtualHostName);
        Assert.Equal(thisHostAddress, instanceOptions.IPAddress);
        Assert.Equal(EurekaInstanceOptions.DefaultAppName, instanceOptions.AppName);
        Assert.Equal(EurekaInstanceOptions.DefaultStatusPageUrlPath, instanceOptions.StatusPageUrlPath);
        Assert.Equal(EurekaInstanceOptions.DefaultHomePageUrlPath, instanceOptions.HomePageUrlPath);
        Assert.Equal(EurekaInstanceOptions.DefaultHealthCheckUrlPath, instanceOptions.HealthCheckUrlPath);
        Assert.NotNull(instanceOptions.MetadataMap);
        Assert.Empty(instanceOptions.MetadataMap);
        Assert.Equal(DataCenterName.MyOwn, instanceOptions.DataCenterInfo.Name);
    }

    [Fact]
    public void Constructor_Initializes_Defaults()
    {
        var instanceOptions = new EurekaInstanceOptions();

        Assert.NotNull(instanceOptions.InstanceId);
        Assert.Equal("unknown", instanceOptions.AppName);
        Assert.Null(instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(80, instanceOptions.NonSecurePort);
        Assert.Equal(443, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
        Assert.False(instanceOptions.IsSecurePortEnabled);
        Assert.Equal(EurekaInstanceOptions.DefaultLeaseRenewalIntervalInSeconds, instanceOptions.LeaseRenewalIntervalInSeconds);
        Assert.Equal(EurekaInstanceOptions.DefaultLeaseExpirationDurationInSeconds, instanceOptions.LeaseExpirationDurationInSeconds);
        Assert.Null(instanceOptions.VirtualHostName);
        Assert.Null(instanceOptions.SecureVirtualHostName);
        Assert.Null(instanceOptions.AsgName);
        Assert.NotNull(instanceOptions.MetadataMap);
        Assert.Empty(instanceOptions.MetadataMap);
        Assert.Equal(EurekaInstanceOptions.DefaultStatusPageUrlPath, instanceOptions.StatusPageUrlPath);
        Assert.Null(instanceOptions.StatusPageUrl);
        Assert.Equal(EurekaInstanceOptions.DefaultHomePageUrlPath, instanceOptions.HomePageUrlPath);
        Assert.Null(instanceOptions.HomePageUrl);
        Assert.Equal(EurekaInstanceOptions.DefaultHealthCheckUrlPath, instanceOptions.HealthCheckUrlPath);
        Assert.Null(instanceOptions.HealthCheckUrl);
        Assert.Null(instanceOptions.SecureHealthCheckUrl);
        Assert.Equal(DataCenterName.MyOwn, instanceOptions.DataCenterInfo.Name);
        Assert.Equal(instanceOptions.GetHostAddress(false), instanceOptions.IPAddress);
        Assert.Empty(instanceOptions.DefaultAddressResolutionOrder);
        Assert.Null(instanceOptions.RegistrationMethod);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.NotNull(instanceOptions.IPAddress);
        }
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
                        "serviceUrl": "http://localhost:8761/eureka/"
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
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        IConfigurationSection instanceSection = configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
        var instanceOptions = new EurekaInstanceOptions();
        instanceSection.Bind(instanceOptions);

        Assert.Equal("instanceId", instanceOptions.InstanceId);
        Assert.Equal("appName", instanceOptions.AppName);
        Assert.Equal("appGroup", instanceOptions.AppGroupName);
        Assert.True(instanceOptions.IsInstanceEnabledOnInit);
        Assert.Equal(100, instanceOptions.NonSecurePort);
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
        Assert.Equal("myHostName", instanceOptions.ResolveHostName(false));
        Assert.Equal("myHostName", instanceOptions.HostName);
        Assert.Equal("foobar", instanceOptions.RegistrationMethod);
        IDictionary<string, string> map = instanceOptions.MetadataMap;
        Assert.NotNull(map);
        Assert.Equal(2, map.Count);
        Assert.Equal("bar", map["foo"]);
        Assert.Equal("foo", map["bar"]);
    }

    [Fact]
    public void Options_DoNotUseInetUtilsByDefault()
    {
        var mockNetUtils = new Mock<InetUtils>(new InetOptions(), NullLogger<InetUtils>.Instance);

        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var instanceOptions = new EurekaInstanceOptions
        {
            NetUtils = mockNetUtils.Object
        };

        configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(instanceOptions);

        mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Never);
    }

    [Fact]
    public void Options_CanUseInetUtils()
    {
        var mockNetUtils = new Mock<InetUtils>(new InetOptions(), NullLogger<InetUtils>.Instance);

        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        var appSettings = new Dictionary<string, string>
        {
            { "eureka:instance:UseNetUtils", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var instanceOptions = new EurekaInstanceOptions
        {
            NetUtils = mockNetUtils.Object
        };

        configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(instanceOptions);

        instanceOptions.ApplyNetUtils();

        Assert.Equal("FromMock", instanceOptions.HostName);
        Assert.Equal("254.254.254.254", instanceOptions.IPAddress);
        mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Once);
    }

    [Fact]
    public void Options_CanUseInetUtilsWithoutReverseDnsOnIP()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "eureka:instance:UseNetUtils", "true" },
            { "spring:cloud:inet:SkipReverseDnsLookup", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var instanceOptions = new EurekaInstanceOptions
        {
            NetUtils = new InetUtils(configurationRoot.GetSection(InetOptions.ConfigurationPrefix).Get<InetOptions>(), NullLogger<InetUtils>.Instance)
        };

        configurationRoot.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(instanceOptions);

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        instanceOptions.ApplyNetUtils();
        noSlowReverseDnsQuery.Stop();

        Assert.NotNull(instanceOptions.HostName);
        Assert.InRange(noSlowReverseDnsQuery.ElapsedMilliseconds, 0, 1500); // testing with an actual reverse dns query results in around 5000 ms
    }

    [Fact]
    public void UpdateConfigurationFindsHttpUrl()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "http://myapp:1233" }
        }).Build();

        var instanceOptions = new EurekaInstanceOptions();

        instanceOptions.ApplyConfigUrls(configurationRoot.GetAspNetCoreUrls());

        Assert.Equal("myapp", instanceOptions.HostName);
        Assert.Equal(1233, instanceOptions.NonSecurePort);
        Assert.False(instanceOptions.IsSecurePortEnabled);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
    }

    [Fact]
    public void UpdateConfigurationFindsUrlsPicksHttps()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" }
        }).Build();

        var instanceOptions = new EurekaInstanceOptions();

        instanceOptions.ApplyConfigUrls(configurationRoot.GetAspNetCoreUrls());

        Assert.Equal("myapp", instanceOptions.HostName);
        Assert.Equal(1234, instanceOptions.SecurePort);
        Assert.Equal(1233, instanceOptions.NonSecurePort);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.False(instanceOptions.IsNonSecurePortEnabled);
    }

    [Fact]
    public void UpdateConfigurationHandlesPlus()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "https://+:443;http://+:80" }
        }).Build();

        var instanceOptions = new EurekaInstanceOptions();

        instanceOptions.ApplyConfigUrls(configurationRoot.GetAspNetCoreUrls());

        Assert.Equal(80, instanceOptions.NonSecurePort);
        Assert.Equal(443, instanceOptions.SecurePort);
        Assert.True(instanceOptions.IsSecurePortEnabled);
        Assert.False(instanceOptions.IsNonSecurePortEnabled);
    }

    [Fact]
    public void UpdateConfigurationUsesDefaultsWhenNoUrl()
    {
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var instanceOptions = new EurekaInstanceOptions();

        instanceOptions.ApplyConfigUrls(configurationRoot.GetAspNetCoreUrls());

        Assert.Equal(80, instanceOptions.NonSecurePort);
        Assert.False(instanceOptions.IsSecurePortEnabled);
        Assert.True(instanceOptions.IsNonSecurePortEnabled);
    }
}
