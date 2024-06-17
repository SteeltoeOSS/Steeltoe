// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    private readonly Dictionary<string, string?> _appSettings = new()
    {
        ["management:endpoints:enabled"] = "false",
        ["management:endpoints:path"] = "/cloudfoundryapplication",
        ["management:endpoints:trace:enabled"] = "false"
    };

    [Fact]
    public void AddTraceActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configurationRoot);

        services.AddTraceActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetService<IOptionsMonitor<TraceEndpointOptions>>();
        Assert.NotNull(options);
        var handler = serviceProvider.GetService<IHttpTraceEndpointHandler>();
        Assert.NotNull(handler);

        IEnumerable<IDiagnosticObserver> observers = serviceProvider.GetServices<IDiagnosticObserver>();
        List<IDiagnosticObserver> list = observers.ToList();
        Assert.Single(list);
        Assert.IsType<HttpTraceDiagnosticObserver>(list[0]);
    }

    [Fact]
    public void AddTraceActuatorV1_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(_appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configurationRoot);

        services.AddTraceActuator(MediaTypeVersion.V1);

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);
        var options = serviceProvider.GetService<IOptionsMonitor<TraceEndpointOptions>>();
        Assert.NotNull(options);
        var handler = serviceProvider.GetService<IHttpTraceEndpointHandler>();
        Assert.NotNull(handler);

        IEnumerable<IDiagnosticObserver> observers = serviceProvider.GetServices<IDiagnosticObserver>();
        List<IDiagnosticObserver> list = observers.ToList();
        Assert.Single(list);
        Assert.IsType<TraceDiagnosticObserver>(list[0]);
    }
}
