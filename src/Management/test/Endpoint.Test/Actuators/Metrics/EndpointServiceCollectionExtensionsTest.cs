// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Actuators.Metrics;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Metrics;

public sealed class EndpointServiceCollectionExtensionsTest : BaseTest
{
    [Fact]
    public async Task AddMetricsActuator_AddsCorrectServices()
    {
        var builder = new ConfigurationBuilder();
        IConfiguration configuration = builder.Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddMetricsActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetService<IDiagnosticsManager>().Should().NotBeNull();
        serviceProvider.GetServices<IHostedService>().OfType<MetricCollectionHostedService>().Should().ContainSingle();

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsObserverOptions>>();
        optionsMonitor.CurrentValue.EgressIgnorePattern.Should().NotBeNullOrEmpty();

        IDiagnosticObserver[] observers = serviceProvider.GetServices<IDiagnosticObserver>().ToArray();
        observers.Should().NotBeEmpty();

        serviceProvider.GetService<IMetricsEndpointHandler>().Should().NotBeNull();
    }
}
