// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public sealed class EndpointServiceCollectionExtensionsTest : BaseTest
{
    [Fact]
    public void AddMetricsActuator_AddsCorrectServices()
    {
        var builder = new ConfigurationBuilder();
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddMetricsActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetService<IDiagnosticsManager>().Should().NotBeNull();
        serviceProvider.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().Should().HaveCount(1);

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsObserverOptions>>();
        optionsMonitor.CurrentValue.EgressIgnorePattern.Should().NotBeNullOrEmpty();

        IDiagnosticObserver[] observers = serviceProvider.GetServices<IDiagnosticObserver>().ToArray();
        observers.Should().NotBeEmpty();

        serviceProvider.GetService<IMetricsEndpointHandler>().Should().NotBeNull();
    }
}
