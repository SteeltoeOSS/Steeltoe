// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Hypermedia;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Hypermedia;

public class EndpointServiceCollectionTest : BaseTest
{
  

    [Fact]
    public void AddHyperMediaActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddHypermediaActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        //var options = serviceProvider.GetService<IActuatorHypermediaOptions>();
        //Assert.NotNull(options);
        var ep = serviceProvider.GetService<IActuatorEndpoint>();
        Assert.NotNull(ep);
    }
}
