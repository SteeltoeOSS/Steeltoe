// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;
using Steeltoe.Common.Utils.IO;
using Xunit;

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

        var optionsMonitor = new TestOptionsMonitor<ConfigurationDiscoveryOptions>(options);
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

        var optionsMonitor = new TestOptionsMonitor<ConfigurationDiscoveryOptions>(options);
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
        const string appsettings = """
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
        string path = sandbox.CreateFile("appsettings.json", appsettings);
        string directory = Path.GetDirectoryName(path)!;
        string fileName = Path.GetFileName(path);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.SetBasePath(directory);
        configurationBuilder.AddJsonFile(fileName);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddConfigurationDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        // by getting the client, we're confirming that the options are also available in the container
        IDiscoveryClient[] discoveryClients = serviceProvider.GetRequiredService<IEnumerable<IDiscoveryClient>>().ToArray();

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
    public void DoesNotRegisterConfigurationDiscoveryClientMultipleTimes()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddConfigurationDiscoveryClient();
        services.AddConfigurationDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IDiscoveryClient[] discoveryClients = serviceProvider.GetRequiredService<IEnumerable<IDiscoveryClient>>().ToArray();
        discoveryClients.OfType<ConfigurationDiscoveryClient>().Should().HaveCount(1);

        ConfigurationDiscoveryClient[] configurationDiscoveryClients =
            serviceProvider.GetRequiredService<IEnumerable<ConfigurationDiscoveryClient>>().ToArray();

        configurationDiscoveryClients.Should().BeEmpty();
    }

    [Fact]
    public void RegistersHostedService()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddConfigurationDiscoveryClient();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IHostedService[] hostedServices = serviceProvider.GetRequiredService<IEnumerable<IHostedService>>().ToArray();
        hostedServices.OfType<DiscoveryClientHostedService>().Should().HaveCount(1);
    }
}
