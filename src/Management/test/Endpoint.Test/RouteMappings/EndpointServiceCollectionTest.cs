// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.RouteMappings;

namespace Steeltoe.Management.Endpoint.Test.RouteMappings;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddMappingsActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(HostingHelpers.GetHostingEnvironment());

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);

        services.AddMappingsActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<RouteMappingsEndpointOptions>>();
        Assert.Equal("mappings", options.CurrentValue.Id);

        var routerMappings = serviceProvider.GetService<RouterMappings>();
        Assert.NotNull(routerMappings);
    }
}
