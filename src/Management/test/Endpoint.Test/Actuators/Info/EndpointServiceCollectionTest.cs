// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddInfoActuator_AddsCorrectServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddInfoActuator();
        services.AddInfoContributor<TestInfoContributor>();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        IInfoContributor[] contributors = serviceProvider.GetServices<IInfoContributor>().ToArray();
        contributors.Should().HaveCount(4);
        contributors.OfType<GitInfoContributor>().Should().NotBeEmpty();
        contributors.OfType<AppSettingsInfoContributor>().Should().NotBeEmpty();
        contributors.OfType<BuildInfoContributor>().Should().NotBeEmpty();
        contributors.OfType<TestInfoContributor>().Should().NotBeEmpty();

        Assert.NotNull(serviceProvider.GetService<IInfoEndpointHandler>());
    }

    [Fact]
    public void AddInfoContributor_DoesNotAddTwice()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddInfoActuator();

        services.RemoveAll<IInfoContributor>();
        services.AddInfoContributor<TestInfoContributor>();
        services.AddInfoContributor<TestInfoContributor>();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetServices<IInfoContributor>().OfType<TestInfoContributor>().Should().HaveCount(1);
    }
}
