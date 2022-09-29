// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Moq;
using Steeltoe.Common.Net;
using Xunit;

namespace Steeltoe.Discovery.Consul.Discovery.Test;

public class ConsulDiscoveryOptionsTest
{
    [Fact]
    public void Constructor_InitializesDefaults()
    {
        var opts = new ConsulDiscoveryOptions();
        Assert.True(opts.Register);
        Assert.True(opts.RegisterHealthCheck);
        Assert.Null(opts.DefaultQueryTag);
        Assert.Equal("zone", opts.DefaultZoneMetadataName);
        Assert.True(opts.Deregister);
        Assert.True(opts.Enabled);
        Assert.True(opts.FailFast);
        Assert.Equal("30m", opts.HealthCheckCriticalTimeout);
        Assert.Equal("10s", opts.HealthCheckInterval);
        Assert.Equal("/actuator/health", opts.HealthCheckPath);
        Assert.Equal("10s", opts.HealthCheckTimeout);
        Assert.False(opts.HealthCheckTlsSkipVerify);
        Assert.Null(opts.HealthCheckUrl);
        Assert.NotNull(opts.Heartbeat);
        Assert.NotNull(opts.HostName);
        Assert.Null(opts.InstanceGroup);
        Assert.Null(opts.InstanceZone);
        Assert.False(opts.PreferIPAddress);
        Assert.False(opts.PreferAgentAddress);
        Assert.False(opts.QueryPassing);
        Assert.Equal("http", opts.Scheme);
        Assert.Null(opts.ServiceName);
        Assert.Null(opts.Tags);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // TODO: this is null on MacOS
            Assert.NotNull(opts.IPAddress);
        }
    }

    [Fact]
    public void Options_DoNotUseInetUtilsByDefault()
    {
        var mockNetUtils = new Mock<InetUtils>(null, null);

        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo
        {
            Hostname = "FromMock",
            IPAddress = "254.254.254.254"
        }).Verifiable();

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var opts = new ConsulDiscoveryOptions
        {
            NetUtils = mockNetUtils.Object
        };

        configurationRoot.GetSection(ConsulDiscoveryOptions.ConsulDiscoveryConfigurationPrefix).Bind(opts);

        opts.ApplyNetUtils();

        mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Never);
    }

    [Fact]
    public void Options_CanUseInetUtils()
    {
        var mockNetUtils = new Mock<InetUtils>(null, null);

        mockNetUtils.Setup(n => n.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo
        {
            Hostname = "FromMock",
            IPAddress = "254.254.254.254"
        }).Verifiable();

        var appSettings = new Dictionary<string, string>
        {
            { "consul:discovery:UseNetUtils", "true" }
        };

        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var opts = new ConsulDiscoveryOptions
        {
            NetUtils = mockNetUtils.Object
        };

        configurationRoot.GetSection(ConsulDiscoveryOptions.ConsulDiscoveryConfigurationPrefix).Bind(opts);

        opts.ApplyNetUtils();

        Assert.Equal("FromMock", opts.HostName);
        Assert.Equal("254.254.254.254", opts.IPAddress);
        mockNetUtils.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Once);
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

        var opts = new ConsulDiscoveryOptions
        {
            NetUtils = new InetUtils(configurationRoot.GetSection(InetOptions.Prefix).Get<InetOptions>())
        };

        configurationRoot.GetSection(ConsulDiscoveryOptions.ConsulDiscoveryConfigurationPrefix).Bind(opts);

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        opts.ApplyNetUtils();
        noSlowReverseDnsQuery.Stop();

        Assert.NotNull(opts.HostName);
        Assert.InRange(noSlowReverseDnsQuery.ElapsedMilliseconds, 0, 1500); // testing with an actual reverse dns query results in around 5000 ms
    }
}
