// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Services;

namespace Steeltoe.Management.Endpoint.Test.Services;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddServicesActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:services:path"] = "/some"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();
        services.AddServicesActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<ServicesEndpointOptions>>();
        Assert.Equal("/some", options.CurrentValue.Path);

        var handler = serviceProvider.GetService<IServicesEndpointHandler>();
        Assert.NotNull(handler);
    }
}
