// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Consul.Discovery;
using Xunit;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class ConsulDiscoveryOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var options = new ConsulDiscoveryOptions();
        Assert.True(options.Register);
        Assert.True(options.RegisterHealthCheck);
        Assert.Null(options.DefaultQueryTag);
        Assert.Equal("zone", options.DefaultZoneMetadataName);
        Assert.True(options.Deregister);
        Assert.True(options.Enabled);
        Assert.True(options.FailFast);
        Assert.Equal("30m", options.HealthCheckCriticalTimeout);
        Assert.Equal("10s", options.HealthCheckInterval);
        Assert.Equal("/actuator/health", options.HealthCheckPath);
        Assert.Equal("10s", options.HealthCheckTimeout);
        Assert.False(options.HealthCheckTlsSkipVerify);
        Assert.Null(options.HealthCheckUrl);
        Assert.NotNull(options.Heartbeat);
        Assert.NotNull(options.HostName);
        Assert.Null(options.InstanceGroup);
        Assert.Null(options.InstanceZone);
        Assert.False(options.PreferIPAddress);
        Assert.False(options.PreferAgentAddress);
        Assert.False(options.QueryPassing);
        Assert.Equal("http", options.Scheme);
        Assert.Null(options.ServiceName);
        Assert.Null(options.Tags);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.NotNull(options.IPAddress);
        }
    }

    [Fact]
    public void Options_DoNotUseInetUtilsByDefault()
    {
        var mockNetUtils = new Mock<InetUtils>(new InetOptions(), NullLogger<InetUtils>.Instance);
        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var options = new ConsulDiscoveryOptions
        {
            NetUtils = mockNetUtils.Object
        };

        configurationRoot.GetSection(ConsulDiscoveryOptions.ConfigurationPrefix).Bind(options);

        options.ApplyNetUtils();

        mockNetUtils.Verify(netUtils => netUtils.FindFirstNonLoopbackHostInfo(), Times.Never);
    }

    [Fact]
    public void Options_CanUseInetUtils()
    {
        var mockNetUtils = new Mock<InetUtils>(new InetOptions(), NullLogger<InetUtils>.Instance);
        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        var appSettings = new Dictionary<string, string>
        {
            { "consul:discovery:UseNetUtils", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var options = new ConsulDiscoveryOptions
        {
            NetUtils = mockNetUtils.Object
        };

        configurationRoot.GetSection(ConsulDiscoveryOptions.ConfigurationPrefix).Bind(options);

        options.ApplyNetUtils();

        Assert.Equal("FromMock", options.HostName);
        Assert.Equal("254.254.254.254", options.IPAddress);
        mockNetUtils.Verify(netUtils => netUtils.FindFirstNonLoopbackHostInfo(), Times.Once);
    }

    [Fact]
    public void Options_CanUseInetUtilsWithoutReverseDnsOnIP()
    {
        var appSettings = new Dictionary<string, string>
        {
            { "consul:discovery:UseNetUtils", "true" },
            { "spring:cloud:inet:SkipReverseDnsLookup", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var options = new ConsulDiscoveryOptions
        {
            NetUtils = new InetUtils(configurationRoot.GetSection(InetOptions.ConfigurationPrefix).Get<InetOptions>(), NullLogger<InetUtils>.Instance)
        };

        configurationRoot.GetSection(ConsulDiscoveryOptions.ConfigurationPrefix).Bind(options);

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        options.ApplyNetUtils();
        noSlowReverseDnsQuery.Stop();

        Assert.NotNull(options.HostName);
        Assert.InRange(noSlowReverseDnsQuery.ElapsedMilliseconds, 0, 1500); // testing with an actual reverse dns query results in around 5000 ms
    }
}
