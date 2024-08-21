// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Actuators.Trace;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Trace;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddTraceActuator_AddsCorrectServices()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:httpTrace:enabled"] = "false"
        };

        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton(configuration);

        services.AddTraceActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<TraceEndpointOptions>>();
        Assert.False(options.CurrentValue.Enabled);

        var handler = serviceProvider.GetService<IHttpTraceEndpointHandler>();
        Assert.NotNull(handler);

        IEnumerable<IDiagnosticObserver> observers = serviceProvider.GetServices<IDiagnosticObserver>();
        List<IDiagnosticObserver> list = observers.ToList();
        Assert.Single(list);
        Assert.IsType<HttpTraceDiagnosticObserver>(list[0]);
    }

    [Fact]
    public async Task AddTraceActuatorV1_AddsCorrectServices()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:trace:enabled"] = "false"
        };

        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfiguration configuration = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton(configuration);

        services.AddTraceActuator(MediaTypeVersion.V1);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<TraceEndpointOptions>>();
        Assert.False(options.Get("V1").Enabled);

        var handler = serviceProvider.GetService<IHttpTraceEndpointHandler>();
        Assert.NotNull(handler);

        IEnumerable<IDiagnosticObserver> observers = serviceProvider.GetServices<IDiagnosticObserver>();
        List<IDiagnosticObserver> list = observers.ToList();
        Assert.Single(list);
        Assert.IsType<TraceDiagnosticObserver>(list[0]);
    }
}
