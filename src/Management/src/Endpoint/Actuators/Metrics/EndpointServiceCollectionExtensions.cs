// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Actuators.Metrics.Observers;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the metrics actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMetricsActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.AddHostedService<DiagnosticsService>();

        services.AddCommonActuatorServices();
        services.AddMetricsActuatorServices();

        services.AddMetricsObservers();

        return services;
    }

    public static IServiceCollection AddMetricsObservers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureOptionsWithChangeTokenSource<MetricsObserverOptions, ConfigureMetricsObserverOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientCoreObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientDesktopObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRuntimeDiagnosticSource, ClrRuntimeObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, EventCounterListener>());

        return services;
    }
}
