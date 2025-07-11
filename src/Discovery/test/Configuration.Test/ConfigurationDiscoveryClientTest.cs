// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.TestResources.IO;

namespace Steeltoe.Discovery.Configuration.Test;

public sealed class ConfigurationDiscoveryClientTest
{
    [Fact]
    public async Task Returns_ConfiguredServices()
    {
        var options = new ConfigurationDiscoveryOptions
        {
            Services =
            {
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruit-ball",
                    Port = 443,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruit-baller",
                    Port = 8081
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruit-ballers",
                    Port = 8082
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableService",
                    Host = "vegemite",
                    Port = 443,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableService",
                    Host = "carrot",
                    Port = 8081
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "vegetableService",
                    Host = "beet",
                    Port = 8082
                }
            }
        };

        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", TestContext.Current.CancellationToken);
        Assert.Equal(3, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", TestContext.Current.CancellationToken);
        Assert.Equal(3, vegetableInstances.Count);

        ISet<string> servicesIds = await client.GetServiceIdsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, servicesIds.Count);
    }

    [Fact]
    public async Task ReceivesUpdatesTo_ConfiguredServices()
    {
        var options = new ConfigurationDiscoveryOptions
        {
            Services =
            {
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruit-ball",
                    Port = 443,
                    IsSecure = true
                }
            }
        };

        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", TestContext.Current.CancellationToken);

        Assert.Single(fruitInstances);
        Assert.Equal("fruit-ball", fruitInstances[0].Host);

        options.Services[0].Host = "updatedValue";

        Assert.Equal("updatedValue", fruitInstances[0].Host);
    }

    [Fact]
    public async Task AddConfigurationDiscoveryClient_AddsClientWithOptions()
    {
        const string appSettings = """
            {
                "discovery": {
                    "services": [
                        { "serviceId": "fruitService", "host": "fruit-ball", "port": 443, "isSecure": true },
                        { "serviceId": "fruitService", "host": "fruit-baller", "port": 8081 },
                        { "serviceId": "vegetableService", "host": "vegemite", "port": 443, "isSecure": true },
                        { "serviceId": "vegetableService", "host": "carrot", "port": 8081 },
                    ]
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile(MemoryFileProvider.DefaultAppSettingsFileName, appSettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // by getting the client, we're confirming that the options are also available in the container
        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];

        Assert.Single(discoveryClients);
        Assert.IsType<ConfigurationDiscoveryClient>(discoveryClients[0]);

        ISet<string> servicesIds = await discoveryClients[0].GetServiceIdsAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, servicesIds.Count);

        IList<IServiceInstance> fruitInstances = await discoveryClients[0].GetInstancesAsync("fruitService", TestContext.Current.CancellationToken);
        Assert.Equal(2, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await discoveryClients[0].GetInstancesAsync("vegetableService", TestContext.Current.CancellationToken);
        Assert.Equal(2, vegetableInstances.Count);
    }

    [Fact]
    public async Task DoesNotRegisterConfigurationDiscoveryClientMultipleTimes()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddConfigurationDiscoveryClient();
        services.AddConfigurationDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IDiscoveryClient[] discoveryClients = [.. serviceProvider.GetServices<IDiscoveryClient>()];
        discoveryClients.OfType<ConfigurationDiscoveryClient>().Should().ContainSingle();

        ConfigurationDiscoveryClient[] configurationDiscoveryClients = [.. serviceProvider.GetServices<ConfigurationDiscoveryClient>()];
        configurationDiscoveryClients.Should().BeEmpty();
    }

    [Fact]
    public async Task RegistersHostedService()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddConfigurationDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IHostedService[] hostedServices = [.. serviceProvider.GetServices<IHostedService>()];
        hostedServices.OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }
}
