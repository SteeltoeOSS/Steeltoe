// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.Actuators.RouteMappings;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddMappingsActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();
        services.AddSingleton(configuration);

        services.AddMappingsActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<RouteMappingsEndpointOptions>>();
        Assert.Equal("mappings", options.CurrentValue.Id);

        var routerMappings = serviceProvider.GetService<RouterMappings>();
        Assert.NotNull(routerMappings);
    }
}
