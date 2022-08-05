// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Moq;
using Steeltoe.Common.Net;
using Steeltoe.Common.Utils.IO;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public class EurekaInstanceOptionsTest : AbstractBaseTest
{
    [Fact]
    public void Constructor_Initializes_Defaults()
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
        Assert.Equal(EurekaInstanceConfig.DefaultLeaseRenewalIntervalInSeconds, opts.LeaseRenewalIntervalInSeconds);
        Assert.Equal(EurekaInstanceConfig.DefaultLeaseExpirationDurationInSeconds, opts.LeaseExpirationDurationInSeconds);
        Assert.Null(opts.VirtualHostName);
        Assert.Null(opts.SecureVirtualHostName);
        Assert.Null(opts.AsgName);
        Assert.NotNull(opts.MetadataMap);
        Assert.Empty(opts.MetadataMap);
        Assert.Equal(EurekaInstanceOptions.DefaultStatusPageUrlPath, opts.StatusPageUrlPath);
        Assert.Null(opts.StatusPageUrl);
        Assert.Equal(EurekaInstanceConfig.DefaultHomePageUrlPath, opts.HomePageUrlPath);
        Assert.Null(opts.HomePageUrl);
        Assert.Equal(EurekaInstanceOptions.DefaultHealthCheckUrlPath, opts.HealthCheckUrlPath);
        Assert.Null(opts.HealthCheckUrl);
        Assert.Null(opts.SecureHealthCheckUrl);
        Assert.Equal(DataCenterName.MyOwn, opts.DataCenterInfo.Name);
        Assert.Equal(opts.GetHostAddress(false), opts.IpAddress);
        Assert.Null(opts.DefaultAddressResolutionOrder);
        Assert.Null(opts.RegistrationMethod);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO: this is null on MacOS
            Assert.NotNull(opts.IpAddress);
        }
    }

    [Fact]
    public void Constructor_ConfiguresEurekaDiscovery_Correctly()
    {
        const string appsettings = @"
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

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path);
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);

        configurationBuilder.AddJsonFile(fileName);
        IConfigurationRoot config = configurationBuilder.Build();

        IConfigurationSection instSection = config.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix);
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
        Assert.Equal("asgName", ro.AsgName);

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
        IDictionary<string, string> map = ro.MetadataMap;
        Assert.NotNull(map);
        Assert.Equal(2, map.Count);
        Assert.Equal("bar", map["foo"]);
        Assert.Equal("foo", map["bar"]);
    }

    [Fact]
    public void Options_DoNotUseInetUtilsByDefault()
    {
        var mockNetUtils = new Mock<InetUtils>(null, null);

        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo
        {
            Hostname = "FromMock",
            IpAddress = "254.254.254.254"
        }).Verifiable();

        IConfigurationRoot config = new ConfigurationBuilder().Build();

        var opts = new EurekaInstanceOptions
        {
            NetUtils = mockNetUtils.Object
        };

        config.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(opts);

        mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Never);
    }

    [Fact]
    public void Options_CanUseInetUtils()
    {
        var mockNetUtils = new Mock<InetUtils>(null, null);

        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo
        {
            Hostname = "FromMock",
            IpAddress = "254.254.254.254"
        }).Verifiable();

        var appSettings = new Dictionary<string, string>
        {
            { "eureka:instance:UseNetUtils", "true" }
        };

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var opts = new EurekaInstanceOptions
        {
            NetUtils = mockNetUtils.Object
        };

        config.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(opts);

        opts.ApplyNetUtils();

        Assert.Equal("FromMock", opts.HostName);
        Assert.Equal("254.254.254.254", opts.IpAddress);
        mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Once);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")] // for some reason this takes 25-ish seconds on the MSFT-hosted MacOS agent
    public void Options_CanUseInetUtilsWithoutReverseDnsOnIP()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "eureka:instance:UseNetUtils", "true" },
            { "spring:cloud:inet:SkipReverseDnsLookup", "true" }
        };

        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var opts = new EurekaInstanceOptions
        {
            NetUtils = new InetUtils(config.GetSection(InetOptions.Prefix).Get<InetOptions>())
        };

        config.GetSection(EurekaInstanceOptions.EurekaInstanceConfigurationPrefix).Bind(opts);

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        opts.ApplyNetUtils();
        noSlowReverseDnsQuery.Stop();

        Assert.NotNull(opts.HostName);
        Assert.InRange(noSlowReverseDnsQuery.ElapsedMilliseconds, 0, 1500); // testing with an actual reverse dns query results in around 5000 ms
    }

    [Fact]
    public void UpdateConfigurationFindsHttpUrl()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "http://myapp:1233" }
        }).Build();

        var instOpts = new EurekaInstanceOptions();

        instOpts.ApplyConfigUrls(config.GetAspNetCoreUrls(), ConfigurationUrlHelpers.WildcardHost);

        Assert.Equal("myapp", instOpts.HostName);
        Assert.Equal(1233, instOpts.Port);
        Assert.False(instOpts.SecurePortEnabled);
        Assert.True(instOpts.NonSecurePortEnabled);
    }

    [Fact]
    public void UpdateConfigurationFindsUrlsPicksHttps()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "https://myapp:1234;http://0.0.0.0:1233;http://::1233;http://*:1233" }
        }).Build();

        var instOpts = new EurekaInstanceOptions();

        instOpts.ApplyConfigUrls(config.GetAspNetCoreUrls(), ConfigurationUrlHelpers.WildcardHost);

        Assert.Equal("myapp", instOpts.HostName);
        Assert.Equal(1234, instOpts.SecurePort);
        Assert.Equal(1233, instOpts.Port);
        Assert.True(instOpts.SecurePortEnabled);
        Assert.False(instOpts.NonSecurePortEnabled);
    }

    [Fact]
    public void UpdateConfigurationHandlesPlus()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            { "urls", "https://+:443;http://+:80" }
        }).Build();

        var instOpts = new EurekaInstanceOptions();

        instOpts.ApplyConfigUrls(config.GetAspNetCoreUrls(), ConfigurationUrlHelpers.WildcardHost);

        Assert.Equal(80, instOpts.Port);
        Assert.Equal(443, instOpts.SecurePort);
        Assert.True(instOpts.SecurePortEnabled);
        Assert.False(instOpts.NonSecurePortEnabled);
    }

    [Fact]
    public void UpdateConfigurationUsesDefaultsWhenNoUrl()
    {
        IConfigurationRoot config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var instOpts = new EurekaInstanceOptions();

        instOpts.ApplyConfigUrls(config.GetAspNetCoreUrls(), ConfigurationUrlHelpers.WildcardHost);

        Assert.Equal(80, instOpts.Port);
        Assert.False(instOpts.SecurePortEnabled);
        Assert.True(instOpts.NonSecurePortEnabled);
    }
}
