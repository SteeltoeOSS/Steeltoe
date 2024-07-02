// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public sealed class EndpointServiceCollectionExtensionsTest : BaseTest
{
    [Fact]
    public void AddMetricsActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = GetConfiguration();

        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(HostingHelpers.GetHostingEnvironment());
        services.AddSingleton(configuration);
        services.AddMetricsActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var diagnosticsManager = serviceProvider.GetService<IDiagnosticsManager>();
        Assert.NotNull(diagnosticsManager);
        var hostedService = serviceProvider.GetService<IHostedService>();
        Assert.NotNull(hostedService);
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsObserverOptions>>();
        Assert.NotNull(optionsMonitor.CurrentValue);

        IEnumerable<IDiagnosticObserver> observers = serviceProvider.GetServices<IDiagnosticObserver>();
        List<IDiagnosticObserver> list = observers.ToList();
        Assert.NotEmpty(list);

        var handler = serviceProvider.GetService<IMetricsEndpointHandler>();
        Assert.NotNull(handler);
    }

    private IConfiguration GetConfiguration()
    {
        var builder = new ConfigurationBuilder();
        return builder.Build();
    }
}
