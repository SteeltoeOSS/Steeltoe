// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Mappings.Test;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddMappingsActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMappingsActuator());
        Assert.Contains(nameof(services), ex.Message);
        Assert.Throws<InvalidOperationException>(() => services2.AddMappingsActuator());
    }

    [Fact]
    public void AddMappingsActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(HostingHelpers.GetHostingEnvironment());

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot config = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(config);

        services.AddMappingsActuator(config);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IMappingsOptions>();
        Assert.NotNull(options);

        var routeMappings = serviceProvider.GetService<IRouteMappings>();
        Assert.NotNull(routeMappings);
    }
}
