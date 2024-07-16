// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul.Test;

public sealed class ConsulServiceCollectionExtensionsTest
{
    [Fact]
    public void AddConsulDiscoveryClient_UsesConsul()
    {
        Dictionary<string, string?> appsettings = new()
        {
            { "spring:application:name", "myName" },
            { "spring:cloud:inet:defaulthostname", "fromtest" },
            { "spring:cloud:inet:skipReverseDnsLookup", "true" },
            { "consul:discovery:UseNetworkInterfaces", "true" },
            { "consul:discovery:register", "false" },
            { "consul:discovery:deregister", "false" },
            { "consul:host", "http://testhost:8500" }
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddConsulDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        IDiscoveryClient[] discoveryClients = serviceProvider.GetServices<IDiscoveryClient>().ToArray();

        Assert.Single(discoveryClients);
        Assert.NotNull(discoveryClients[0]);
        Assert.IsType<ConsulDiscoveryClient>(discoveryClients[0]);
    }

    [Fact]
    public void ClientEnabledByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddConsulDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);
        var clientOptions = provider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientDisabledBySpringCloudDiscoveryEnabledFalse()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var appSettings = new Dictionary<string, string?>
        {
            { "spring:cloud:discovery:enabled", "false" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());
        services.AddConsulDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);
        var clientOptions = provider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

        Assert.False(clientOptions.Value.Enabled);
    }

    [Fact]
    public void ClientFavorsConsulDiscoveryEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var appSettings = new Dictionary<string, string?>
        {
            { "spring:cloud:discovery:enabled", "false" },
            { "consul:discovery:enabled", "true" }
        };

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build());

        services.AddConsulDiscoveryClient();

        ServiceProvider provider = services.BuildServiceProvider(true);
        var clientOptions = provider.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();

        Assert.True(clientOptions.Value.Enabled);
    }

    [Fact]
    public void DoesNotRegisterConsulDiscoveryClientMultipleTimes()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:register"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();

        services.AddConsulDiscoveryClient();
        services.AddConsulDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IDiscoveryClient>().OfType<ConsulDiscoveryClient>().Should().HaveCount(1);
        serviceProvider.GetServices<ConsulDiscoveryClient>().Should().BeEmpty();
        serviceProvider.GetServices<IHealthContributor>().OfType<ConsulHealthContributor>().Should().HaveCount(1);
    }

    [Fact]
    public void RegistersHostedService()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["consul:discovery:register"] = "false"
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddConsulDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        hostedServices.OfType<DiscoveryClientHostedService>().Should().HaveCount(1);
    }

    [Fact]
    public void ConsulOptionsValidation_FailsWhenRunningInCloudWithLocalhost()
    {
        using var scope = new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddConsulDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var consulOptions = serviceProvider.GetRequiredService<IOptions<ConsulOptions>>();

        Action action = () => _ = consulOptions.Value;

        action.Should().ThrowExactly<OptionsValidationException>().WithMessage(
            "Consul URL 'http://localhost:8500' is not valid in containerized or cloud environments. " +
            "Please configure Consul:Host with a non-localhost server.");
    }
}
