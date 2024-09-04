// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Net;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Registry;
using HostInfo = Steeltoe.Common.Net.HostInfo;

namespace Steeltoe.Discovery.Consul.Test;

public sealed class NoProjectReferenceToCloudFoundryConfigurationTest
{
    [Fact]
    public async Task CanConfigure()
    {
        var inetUtilsMock = new Mock<InetUtils>(new TestOptionsMonitor<InetOptions>(), NullLogger<InetUtils>.Instance);
        inetUtilsMock.Setup(inetUtils => inetUtils.FindFirstNonLoopbackHostInfo()).Returns(new HostInfo("FromMock", "254.254.254.254")).Verifiable();

        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddSingleton(inetUtilsMock.Object);
        services.AddApplicationInstanceInfo();
        services.AddOptions<ConsulDiscoveryOptions>().BindConfiguration(ConsulDiscoveryOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<ConsulDiscoveryOptions>, PostConfigureConsulDiscoveryOptions>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<ConsulDiscoveryOptions>>();

        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;

        options.InstanceId.Should().StartWith(Assembly.GetEntryAssembly()!.GetName().Name);
    }

    [Fact]
    public async Task CreatesConsulServices()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:serviceName"] = "testhost",
            ["consul:discovery:enabled"] = "true",
            ["consul:discovery:failfast"] = "true",
            ["consul:discovery:register"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddConsulDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        serviceProvider.GetServices<IDiscoveryClient>().OfType<ConsulDiscoveryClient>().Should().HaveCount(1);

        _ = serviceProvider.GetRequiredService<IConsulClient>();
        _ = serviceProvider.GetRequiredService<TtlScheduler>();
        _ = serviceProvider.GetRequiredService<ConsulServiceRegistry>();
        _ = serviceProvider.GetRequiredService<ConsulRegistration>();
        _ = serviceProvider.GetRequiredService<ConsulServiceRegistrar>();
        _ = serviceProvider.GetRequiredService<IHealthContributor>();
    }
}
