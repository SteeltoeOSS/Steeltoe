// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Configuration;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test.Discovery;

public sealed class PostConfigureConsulDiscoveryOptionsTest
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
        Assert.Null(options.HostName);
        Assert.Null(options.InstanceGroup);
        Assert.Null(options.InstanceZone);
        Assert.False(options.PreferIPAddress);
        Assert.True(options.QueryPassing);
        Assert.Equal("http", options.Scheme);
        Assert.Null(options.ServiceName);
        Assert.Empty(options.Tags);
        Assert.Empty(options.Metadata);
        Assert.Null(options.IPAddress);
    }

    [Fact]
    public void DoesNotUseNetworkInterfacesByDefault()
    {
        var inetUtilsMock = new Mock<InetUtils>(new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(inetUtilsMock.Object);
        services.ConfigureReloadableOptions<ConsulDiscoveryOptions>(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        _ = optionsMonitor.CurrentValue;

        inetUtilsMock.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Never);
    }

    [Fact]
    public void CanUseNetworkInterfaces()
    {
        var inetUtilsMock = new Mock<InetUtils>(new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        var appSettings = new Dictionary<string, string?>
        {
            { "consul:discovery:UseNetworkInterfaces", "true" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(inetUtilsMock.Object);
        services.ConfigureReloadableOptions<ConsulDiscoveryOptions>(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;

        Assert.Equal("FromMock", options.HostName);
        Assert.Equal("254.254.254.254", options.IPAddress);
        inetUtilsMock.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Once);
    }

    [Fact]
    public void CanUseNetworkInterfacesWithoutReverseDnsOnIP()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "consul:discovery:UseNetworkInterfaces", "true" },
            { "spring:cloud:inet:SkipReverseDnsLookup", "true" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton<InetUtils>();
        services.ConfigureReloadableOptions<ConsulDiscoveryOptions>(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;
        noSlowReverseDnsQuery.Stop();

        Assert.NotNull(options.HostName);
        Assert.InRange(noSlowReverseDnsQuery.ElapsedMilliseconds, 0, 1500); // testing with an actual reverse dns query results in around 5000 ms
    }
}
