// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Services;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Services;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddServicesActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        var ex = Assert.Throws<ArgumentNullException>(() => services.AddServicesActuator());
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddServicesActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();
        services.AddServicesActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<ServicesEndpointOptions>>();
        Assert.NotNull(options);
        var ep = serviceProvider.GetService<IServicesEndpointHandler>();
        Assert.NotNull(ep);
    }
}
