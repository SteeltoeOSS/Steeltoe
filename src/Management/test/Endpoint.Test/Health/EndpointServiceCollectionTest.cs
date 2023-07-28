// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.Availability;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddHealthActuator_AddsCorrectServicesWithDefaultHealthAggregator()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:health:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddLogging();
        services.AddHealthActuator(new DefaultHealthAggregator(), typeof(DiskSpaceContributor));

        services.Configure<HealthCheckServiceOptions>(configurationRoot);
        services.AddSingleton<IConfiguration>(configurationRoot);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        var aggregator = serviceProvider.GetService<IHealthAggregator>();
        Assert.NotNull(aggregator);
        IEnumerable<IHealthContributor> contributors = serviceProvider.GetServices<IHealthContributor>();
        Assert.NotNull(contributors);
        List<IHealthContributor> contributorList = contributors.ToList();
        Assert.Single(contributorList);

        var handler = serviceProvider.GetService<IHealthEndpointHandler>();
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddHealthActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:health:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddHealthActuator();

        services.Configure<HealthCheckServiceOptions>(configurationRoot);
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetService<IHealthEndpointHandler>();
        Assert.NotNull(handler);
        var aggregator = serviceProvider.GetService<IHealthAggregator>();
        Assert.NotNull(aggregator);
        IEnumerable<IHealthContributor> contributors = serviceProvider.GetServices<IHealthContributor>();
        Assert.NotNull(contributors);
        List<IHealthContributor> contributorsList = contributors.ToList();
        Assert.Equal(3, contributorsList.Count);
        var availability = serviceProvider.GetService<ApplicationAvailability>();
        Assert.NotNull(availability);
    }

    [Fact]
    public void AddHealthContributors_AddsServices()
    {
        var services = new ServiceCollection();
        services.AddHealthContributors(typeof(TestContributor));
        ServiceProvider serviceProvider = services.BuildServiceProvider();
        IEnumerable<IHealthContributor> contributors = serviceProvider.GetServices<IHealthContributor>();
        Assert.NotNull(contributors);
        List<IHealthContributor> contributorsList = contributors.ToList();
        Assert.Single(contributorsList);
    }
}
