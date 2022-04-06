// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Diagnostics.Tracing;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public static class EndpointServiceCollectionExtensions
    {
        public static void AddMetricsActuator(this IServiceCollection services, IConfiguration config = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            config ??= services.BuildServiceProvider().GetService<IConfiguration>();
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());

            services.AddActuatorManagementOptions(config);
            services.AddMetricsActuatorServices(config);

            var observerOptions = new MetricsObserverOptions(config);
            services.TryAddSingleton<IMetricsObserverOptions>(observerOptions);

            services.TryAddSingleton<IViewRegistry, ViewRegistry>();
            AddMetricsObservers(services, observerOptions);

            services.AddActuatorEndpointMapping<MetricsEndpoint>();
        }

        public static void AddPrometheusActuator(this IServiceCollection services, IConfiguration config = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            config ??= services.BuildServiceProvider().GetService<IConfiguration>();
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(config)));

            var metricsEndpointOptions = new MetricsEndpointOptions(config);
            services.TryAddSingleton<IMetricsEndpointOptions>(metricsEndpointOptions);

            var observerOptions = new MetricsObserverOptions(config);
            services.TryAddSingleton<IMetricsObserverOptions>(observerOptions);
            services.TryAddSingleton<IViewRegistry, ViewRegistry>();
            services.TryAddSingleton<PrometheusEndpointOptions>();

            services.AddPrometheusActuatorServices(config);

            AddMetricsObservers(services, observerOptions);

            services.AddActuatorEndpointMapping<PrometheusScraperEndpoint>();
        }

        /// <summary>
        /// Adds the services used by the Wavefront exporter
        /// </summary>
        /// <param name="services">Reference to the service collection</param>
        /// <param name="configuration">Reference to the configuration system</param>
        /// <returns>A reference to the service collection</returns>
        public static IServiceCollection AddWavefrontMetrics(this IServiceCollection services, IConfiguration configuration = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            configuration ??= services.BuildServiceProvider().GetService<IConfiguration>();
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());

            var observerOptions = new MetricsObserverOptions(configuration);
            services.TryAddSingleton<IMetricsObserverOptions>(observerOptions);
            services.TryAddSingleton<IViewRegistry, ViewRegistry>();

            AddMetricsObservers(services, observerOptions);

            services.TryAddSingleton(provider =>
            {
                var options = provider.GetService<IMetricsEndpointOptions>();
                var logger = provider.GetService<ILogger<WavefrontMetricsExporter>>();
                return new WavefrontMetricsExporter(new WavefrontExporterOptions(configuration), logger);
            });

            services.AddOpenTelemetryMetricsForSteeltoe();

            return services;
        }

        internal static void AddMetricsObservers(IServiceCollection services, MetricsObserverOptions observerOptions)
        {
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
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IRuntimeDiagnosticSource, CLRRuntimeObserver>());
            }

            if (observerOptions.EventCounterEvents)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, EventCounterListener>());
            }

            if (observerOptions.HystrixEvents)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, HystrixEventsListener>());
            }
        }
    }
}
