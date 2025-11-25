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
        fruitInstances.Should().HaveCount(3);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", TestContext.Current.CancellationToken);
        vegetableInstances.Should().HaveCount(3);

        ISet<string> servicesIds = await client.GetServiceIdsAsync(TestContext.Current.CancellationToken);
        servicesIds.Should().HaveCount(2);
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

        fruitInstances.Should().ContainSingle().Which.Host.Should().Be("fruit-ball");

        options.Services[0].Host = "updatedValue";

        fruitInstances.Should().ContainSingle().Which.Host.Should().Be("updatedValue");
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
        ConfigurationDiscoveryClient client = serviceProvider.GetServices<IDiscoveryClient>().Should().ContainSingle().Which.Should()
            .BeOfType<ConfigurationDiscoveryClient>().Subject;

        ISet<string> servicesIds = await client.GetServiceIdsAsync(TestContext.Current.CancellationToken);
        servicesIds.Should().HaveCount(2);

        IList<IServiceInstance> fruitInstances = await client.GetInstancesAsync("fruitService", TestContext.Current.CancellationToken);
        fruitInstances.Should().HaveCount(2);

        IList<IServiceInstance> vegetableInstances = await client.GetInstancesAsync("vegetableService", TestContext.Current.CancellationToken);
        vegetableInstances.Should().HaveCount(2);
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

        serviceProvider.GetServices<IDiscoveryClient>().Should().ContainSingle().Which.Should().BeOfType<ConfigurationDiscoveryClient>();
        serviceProvider.GetServices<ConfigurationDiscoveryClient>().Should().BeEmpty();
    }

    [Fact]
    public async Task RegistersHostedService()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);

        services.AddConfigurationDiscoveryClient();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IHostedService>().OfType<DiscoveryClientHostedService>().Should().ContainSingle();
    }

    [Fact]
    public void Does_not_register_multiple_times()
    {
        var services = new ServiceCollection();
        services.AddConfigurationDiscoveryClient();
        int beforeServiceCount = services.Count;

        services.AddConfigurationDiscoveryClient();

        services.Count.Should().Be(beforeServiceCount);
    }
}
