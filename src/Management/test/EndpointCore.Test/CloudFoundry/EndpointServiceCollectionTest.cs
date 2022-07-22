// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddCloudFoundryActuator_ThrowsOnNulls()
    {
        IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();
        IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddCloudFoundryActuator(services, config));
        Assert.Contains(nameof(services), ex.Message);
        var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddCloudFoundryActuator(services2, config));
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddCloudFoundryActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        var appSettings = new Dictionary<string, string>()
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:info:enabled"] = "true"
        };
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        var config = configurationBuilder.Build();

        services.AddCloudFoundryActuator(config);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<ICloudFoundryOptions>();
        Assert.NotNull(options);
        var ep = serviceProvider.GetService<CloudFoundryEndpoint>();
        Assert.NotNull(ep);
    }
}