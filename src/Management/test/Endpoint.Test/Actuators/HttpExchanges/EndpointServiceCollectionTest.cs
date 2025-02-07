// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddHttpExchangesActuator_AddsCorrectServices()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:httpExchanges:enabled"] = "false"
        };

        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton(configuration);

        services.AddHttpExchangesActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HttpExchangesEndpointOptions>>();
        options.CurrentValue.Enabled.Should().BeFalse();

        var handler = serviceProvider.GetService<IHttpExchangesEndpointHandler>();
        handler.Should().NotBeNull();

        IEnumerable<DiagnosticObserver> observers = serviceProvider.GetServices<DiagnosticObserver>();
        List<DiagnosticObserver> list = observers.ToList();
        list.Should().ContainSingle();
        list[0].Should().BeOfType<HttpExchangesDiagnosticObserver>();
    }
}
