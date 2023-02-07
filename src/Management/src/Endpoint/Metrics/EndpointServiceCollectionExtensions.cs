// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Extensions;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics.Observer;

namespace Steeltoe.Management.Endpoint.Metrics;

public static class EndpointServiceCollectionExtensions
{
    public static void AddMetricsActuator(this IServiceCollection services, IConfiguration configuration = null)
    {
        ArgumentGuard.NotNull(services);

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());

        services.AddActuatorManagementOptions(configuration);
        services.AddMetricsActuatorServices(configuration);

        var observerOptions = new MetricsObserverOptions(configuration);
        services.TryAddSingleton<IMetricsObserverOptions>(observerOptions);
        services.AddMetricsObservers();

        services.AddActuatorEndpointMapping<MetricsEndpoint>();
    }


    public static void AddMetricsObservers(this IServiceCollection services)
    {
        var configuration = services.BuildServiceProvider().GetService<IConfiguration>();
        var observerOptions = new MetricsObserverOptions(configuration);

        if (observerOptions.AspNetCoreHosting)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
        }

        if (observerOptions.HttpClientCore)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientCoreObserver>());
        }

        if (observerOptions.HttpClientDesktop)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientDesktopObserver>());
        }

        if (observerOptions.GCEvents || observerOptions.ThreadPoolEvents)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRuntimeDiagnosticSource, ClrRuntimeObserver>());
        }

        if (observerOptions.EventCounterEvents)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, EventCounterListener>());
        }
    }
}
