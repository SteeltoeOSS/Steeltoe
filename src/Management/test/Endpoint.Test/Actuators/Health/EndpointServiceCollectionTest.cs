// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Actuators.Health.Availability;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddHealthActuator_AddsCorrectServicesWithDefaultHealthAggregator()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:health:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthActuator();
        services.Configure<HealthCheckServiceOptions>(configuration);
        services.AddSingleton(configuration);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = serviceProvider.GetService<IHealthEndpointHandler>();
        Assert.NotNull(handler);

        var aggregator = serviceProvider.GetService<IHealthAggregator>();
        Assert.NotNull(aggregator);

        IEnumerable<IHealthContributor> contributors = serviceProvider.GetServices<IHealthContributor>();
        Assert.NotEmpty(contributors);
    }

    [Fact]
    public async Task AddHealthActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:health:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHealthActuator();

        services.Configure<HealthCheckServiceOptions>(configuration);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = serviceProvider.GetService<IHealthEndpointHandler>();
        Assert.NotNull(handler);

        var aggregator = serviceProvider.GetService<IHealthAggregator>();
        Assert.NotNull(aggregator);

        IEnumerable<IHealthContributor> contributors = serviceProvider.GetServices<IHealthContributor>();
        Assert.Equal(3, contributors.Count());

        var availability = serviceProvider.GetService<ApplicationAvailability>();
        Assert.NotNull(availability);
    }

    [Fact]
    public async Task AddHealthContributors_AddsNoDuplicates()
    {
        var services = new ServiceCollection();
        services.AddHealthContributor<TestHealthContributor>();
        services.AddHealthContributor<TestHealthContributor>();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IEnumerable<IHealthContributor> contributors = serviceProvider.GetServices<IHealthContributor>();
        Assert.Single(contributors);
    }
}
