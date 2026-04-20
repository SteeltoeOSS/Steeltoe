// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.TestResources;

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

        var fileProvider = new MemoryFileProvider();
        fileProvider.IncludeAppSettingsJsonFile(appSettings);

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryAppSettingsJsonFile(fileProvider);
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

    [Fact]
    public async Task InstancesFetched_event_is_raised_after_configuration_change()
    {
        var fileProvider = new MemoryFileProvider();

        fileProvider.IncludeAppSettingsJsonFile("""
            {
              "Discovery": {
                "Services": [
                  {
                    "ServiceId": "serviceA",
                    "host": "instanceA1",
                    "port": 443,
                    "isSecure": true
                  },
                  {
                    "ServiceId": "serviceA",
                    "host": "instanceA2",
                    "port": 443,
                    "isSecure": true
                  },
                  {
                    "ServiceId": "serviceB",
                    "host": "instanceB1",
                    "port": 443,
                    "isSecure": true
                  }
                ]
              }
            }
            """);

        WebApplicationBuilder builder = TestWebApplicationBuilderFactory.Create();
        builder.Configuration.AddInMemoryAppSettingsJsonFile(fileProvider);
        builder.Services.AddConfigurationDiscoveryClient();
        await using WebApplication webApplication = builder.Build();

        ConfigurationDiscoveryClient discoveryClient = webApplication.Services.GetServices<IDiscoveryClient>().OfType<ConfigurationDiscoveryClient>().Single();
        DiscoveryInstancesFetchedEventArgs? eventArgs = null;
        int eventCount = 0;

        discoveryClient.InstancesFetched += (_, args) =>
        {
            eventArgs = args;
            Interlocked.Increment(ref eventCount);
        };

        fileProvider.ReplaceAppSettingsJsonFile("""
            {
              "Discovery": {
                "Services": [
                  {
                    "ServiceId": "serviceA",
                    "host": "instanceA1",
                    "port": 443,
                    "isSecure": true
                  },
                  {
                    "ServiceId": "serviceB",
                    "host": "instanceB1",
                    "port": 443,
                    "isSecure": true
                  },
                  {
                    "ServiceId": "serviceB",
                    "host": "instanceB2",
                    "port": 443,
                    "isSecure": true
                  }
                ]
              }
            }
            """);

        fileProvider.NotifyChanged();

        SpinWait.SpinUntil(() => eventCount == 1, 5.Seconds()).Should().BeTrue();

        eventArgs.Should().NotBeNull();
        eventArgs.InstancesByServiceId.Should().HaveCount(2);
        eventArgs.InstancesByServiceId.Should().ContainKey("ServiceA").WhoseValue.Should().HaveCount(1);
        eventArgs.InstancesByServiceId.Should().ContainKey("ServiceB").WhoseValue.Should().HaveCount(2);
    }
}
