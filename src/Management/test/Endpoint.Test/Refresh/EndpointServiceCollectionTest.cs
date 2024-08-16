// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Refresh;

namespace Steeltoe.Management.Endpoint.Test.Refresh;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddRefreshActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:refresh:path"] = "/some"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();
        services.AddSingleton(configuration);
        services.AddLogging();
        services.AddRefreshActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<RefreshEndpointOptions>>();
        Assert.Equal("/some", options.CurrentValue.Path);

        var handler = serviceProvider.GetService<IRefreshEndpointHandler>();
        Assert.NotNull(handler);
    }
}
