// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.CloudFoundry;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddCloudFoundryActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddCloudFoundryActuator());
        Assert.Contains(nameof(services), ex.Message);
        Assert.Throws<InvalidOperationException>(() => services2.AddCloudFoundryActuator());
    }

    [Fact]
    public void AddCloudFoundryActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:info:enabled"] = "true"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddCloudFoundryActuator(configurationRoot);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<ICloudFoundryOptions>();
        Assert.NotNull(options);
        var ep = serviceProvider.GetService<CloudFoundryEndpoint>();
        Assert.NotNull(ep);
    }
}
