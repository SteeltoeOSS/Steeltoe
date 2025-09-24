// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Extensions;
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

        options.Register.Should().BeTrue();
        options.RegisterHealthCheck.Should().BeTrue();
        options.DefaultQueryTag.Should().BeNull();
        options.DefaultZoneMetadataName.Should().Be("zone");
        options.Deregister.Should().BeTrue();
        options.Enabled.Should().BeTrue();
        options.FailFast.Should().BeTrue();
        options.HealthCheckCriticalTimeout.Should().Be("30m");
        options.HealthCheckInterval.Should().Be("10s");
        options.HealthCheckPath.Should().Be("/actuator/health");
        options.HealthCheckTimeout.Should().Be("10s");
        options.HealthCheckTlsSkipVerify.Should().BeFalse();
        options.HealthCheckUrl.Should().BeNull();
        options.Heartbeat.Should().NotBeNull();
        options.HostName.Should().BeNull();
        options.InstanceGroup.Should().BeNull();
        options.InstanceZone.Should().BeNull();
        options.PreferIPAddress.Should().BeFalse();
        options.QueryPassing.Should().BeTrue();
        options.Scheme.Should().BeNull();
        options.EffectiveScheme.Should().Be("http");
        options.ServiceName.Should().BeNull();
        options.Tags.Should().BeEmpty();
        options.Metadata.Should().BeEmpty();
        options.IPAddress.Should().BeNull();
    }

    [Fact]
    public async Task DoesNotUseNetworkInterfacesByDefault()
    {
        var domainNameResolverMock = new Mock<IDomainNameResolver>();
        var inetUtilsMock = new Mock<InetUtils>(domainNameResolverMock.Object, new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(domainNameResolverMock.Object);
        services.AddSingleton(inetUtilsMock.Object);
        services.AddApplicationInstanceInfo();
        services.AddOptions<ConsulDiscoveryOptions>().BindConfiguration(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        _ = optionsMonitor.CurrentValue;

        inetUtilsMock.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Never);
    }

    [Fact]
    public async Task CanUseNetworkInterfaces()
    {
        var domainNameResolverMock = new Mock<IDomainNameResolver>();
        var inetUtilsMock = new Mock<InetUtils>(domainNameResolverMock.Object, new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:UseNetworkInterfaces"] = "true"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(domainNameResolverMock.Object);
        services.AddSingleton(inetUtilsMock.Object);
        services.AddApplicationInstanceInfo();
        services.AddOptions<ConsulDiscoveryOptions>().BindConfiguration(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;

        options.HostName.Should().Be("FromMock");
        options.IPAddress.Should().Be("254.254.254.254");

        inetUtilsMock.Verify(n => n.FindFirstNonLoopbackHostInfo(), Times.Once);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public async Task CanUseNetworkInterfacesWithoutReverseDnsOnIP()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:UseNetworkInterfaces"] = "true",
            ["spring:cloud:inet:SkipReverseDnsLookup"] = "true"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton<IDomainNameResolver>(DomainNameResolver.Instance);
        services.AddSingleton<InetUtils>();
        services.AddApplicationInstanceInfo();
        services.AddOptions<ConsulDiscoveryOptions>().BindConfiguration(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        var noSlowReverseDnsQuery = new Stopwatch();
        noSlowReverseDnsQuery.Start();
        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;
        noSlowReverseDnsQuery.Stop();

        options.HostName.Should().NotBeNull();
        noSlowReverseDnsQuery.ElapsedMilliseconds.Should().BeInRange(0, 1500); // testing with an actual reverse dns query results in around 5000 ms
    }

    [Fact]
    public void NormalizeForConsul_Digit_Throws()
    {
        Action action = () => PostConfigureConsulDiscoveryOptions.NormalizeForConsul("9abc", "name");

        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeForConsul_ColonAtStart_Throws()
    {
        Action action = () => PostConfigureConsulDiscoveryOptions.NormalizeForConsul(":abc", "name");

        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeForConsul_ColonAtEnd_Throws()
    {
        Action action = () => PostConfigureConsulDiscoveryOptions.NormalizeForConsul("abc:", "name");

        action.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeForConsul_ReturnsExpected()
    {
        PostConfigureConsulDiscoveryOptions.NormalizeForConsul("abc1", "name").Should().Be("abc1");
        PostConfigureConsulDiscoveryOptions.NormalizeForConsul("ab:c1", "name").Should().Be("ab-c1");
        PostConfigureConsulDiscoveryOptions.NormalizeForConsul("ab::c1", "name").Should().Be("ab-c1");
    }
}
