// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class NoProjectReferenceToCloudFoundryConfigurationTest
{
    [Fact]
    public async Task CanConfigure()
    {
        string? hostName = DnsTools.ResolveHostName();
        string appName = Assembly.GetEntryAssembly()!.GetName().Name!;

        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton<InetUtils>();
        services.AddApplicationInstanceInfo();
        services.AddOptions<EurekaInstanceOptions>().BindConfiguration(EurekaInstanceOptions.ConfigurationPrefix);
        services.AddSingleton<IPostConfigureOptions<EurekaInstanceOptions>, PostConfigureEurekaInstanceOptions>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<EurekaInstanceOptions>>();
        EurekaInstanceOptions instanceOptions = optionsMonitor.CurrentValue;

        instanceOptions.InstanceId.Should().Be($"{hostName}:{appName}:{5000}");
    }

    [Fact]
    public async Task CreatesEurekaServices()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "true",
            ["eureka:client:shouldRegisterWithEureka"] = "false",
            ["eureka:client:shouldFetchRegistry"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddEurekaDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IDiscoveryClient>().OfType<EurekaDiscoveryClient>().Should().HaveCount(1);
        serviceProvider.GetServices<EurekaDiscoveryClient>().Should().HaveCount(1);
        serviceProvider.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().Should().HaveCount(1);
        serviceProvider.GetServices<IHealthContributor>().OfType<EurekaServerHealthContributor>().Should().HaveCount(1);
        serviceProvider.GetRequiredService<IApplicationInstanceInfo>().Should().BeOfType<ApplicationInstanceInfo>();
        serviceProvider.GetRequiredService<IHealthCheckHandler>().Should().BeOfType<EurekaHealthCheckHandler>();
        _ = serviceProvider.GetRequiredService<EurekaClient>();
    }
}
