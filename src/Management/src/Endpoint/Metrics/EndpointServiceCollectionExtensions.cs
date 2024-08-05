// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics.Observer;

namespace Steeltoe.Management.Endpoint.Metrics;

public static class EndpointServiceCollectionExtensions
{
    public static void AddMetricsActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.AddHostedService<DiagnosticsService>();

        services.AddCommonActuatorServices();
        services.AddMetricsActuatorServices();

        services.AddMetricsObservers();
    }

    public static void AddMetricsObservers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureOptionsWithChangeTokenSource<MetricsObserverOptions, ConfigureMetricsObserverOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientCoreObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientDesktopObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRuntimeDiagnosticSource, ClrRuntimeObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, EventCounterListener>());
    }
}
