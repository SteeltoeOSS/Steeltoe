// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class EndpointServiceCollectionExtensionsTest : BaseTest
{
    [Fact]
    public async Task AddRouteMappingsActuator_AddsCorrectServices()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["Management:Endpoints:Mappings:Enabled"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddSingleton<IHostEnvironment, HostingEnvironment>();
        services.AddRouteMappingsActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RouteMappingsEndpointOptions>>();
        optionsMonitor.CurrentValue.Enabled.Should().BeFalse();

        serviceProvider.GetService<IRouteMappingsEndpointHandler>().Should().BeOfType<RouteMappingsEndpointHandler>();
        serviceProvider.GetService<AspNetEndpointProvider>().Should().NotBeNull();
        serviceProvider.GetService<ActuatorRouteOptionsResolver>().Should().NotBeNull();
    }
}
