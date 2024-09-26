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
                    Host = "fruitball",
                    Port = 443,
                    IsSecure = true
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruitballer",
                    Port = 8081
                },
                new ConfigurationServiceInstance
                {
                    ServiceId = "fruitService",
                    Host = "fruitballerz",
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

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(3, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", CancellationToken.None);
        Assert.Equal(3, vegetableInstances.Count);

        ISet<string> servicesIds = await client.GetServiceIdsAsync(CancellationToken.None);
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
                    Host = "fruitball",
                    Port = 443,
                    IsSecure = true
                }
            }
        };

        TestOptionsMonitor<ConfigurationDiscoveryOptions> optionsMonitor = TestOptionsMonitor.Create(options);
        var client = new ConfigurationDiscoveryClient(optionsMonitor);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", CancellationToken.None);

        Assert.Single(fruitInstances);
        Assert.Equal("fruitball", fruitInstances[0].Host);

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
                        { "serviceId": "fruitService", "host": "fruitball", "port": 443, "isSecure": true },
                        { "serviceId": "fruitService", "host": "fruitballer", "port": 8081 },
                        { "serviceId": "vegetableService", "host": "vegemite", "port": 443, "isSecure": true },
                        { "serviceId": "vegetableService", "host": "carrot", "port": 8081 },
                    ]
                }
            }
            """;

        using var sandbox = new Sandbox();
        string path = sandbox.CreateFile("appsettings.json", appSettings);
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
        IDiscoveryClient[] discoveryClients = serviceProvider.GetServices<IDiscoveryClient>().ToArray();

        Assert.Single(discoveryClients);
        Assert.IsType<ConfigurationDiscoveryClient>(discoveryClients[0]);

        ISet<string> servicesIds = await discoveryClients[0].GetServiceIdsAsync(CancellationToken.None);
        Assert.Equal(2, servicesIds.Count);

        IList<IServiceInstance> fruitInstances = await discoveryClients[0].GetInstancesAsync("fruitService", CancellationToken.None);
        Assert.Equal(2, fruitInstances.Count);

        IList<IServiceInstance> vegetableInstances = await discoveryClients[0].GetInstancesAsync("vegetableService", CancellationToken.None);
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

        IDiscoveryClient[] discoveryClients = serviceProvider.GetServices<IDiscoveryClient>().ToArray();
        discoveryClients.OfType<ConfigurationDiscoveryClient>().Should().HaveCount(1);

        ConfigurationDiscoveryClient[] configurationDiscoveryClients = serviceProvider.GetServices<ConfigurationDiscoveryClient>().ToArray();
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

        IHostedService[] hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        hostedServices.OfType<DiscoveryClientHostedService>().Should().HaveCount(1);
    }
}
