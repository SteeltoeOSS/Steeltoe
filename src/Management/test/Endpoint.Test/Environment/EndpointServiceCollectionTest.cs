// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Environment;

namespace Steeltoe.Management.Endpoint.Test.Environment;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddEnvironmentActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        IHostEnvironment host = HostingHelpers.GetHostingEnvironment();
        services.AddSingleton(host);

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:env:path"] = "/some"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddLogging();
        services.AddEnvironmentActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<EnvironmentEndpointOptions>>();
        Assert.Equal("/some", options.CurrentValue.Path);

        var handler = serviceProvider.GetService<IEnvironmentEndpointHandler>();
        Assert.NotNull(handler);
    }
}
