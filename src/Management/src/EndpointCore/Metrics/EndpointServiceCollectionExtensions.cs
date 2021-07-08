// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using Steeltoe.Management.OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using Steeltoe.Management.OpenTelemetry.Stats;
using System;
using System.Diagnostics.Tracing;
using System.Linq;

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

            AddMetricsObservers(services, observerOptions);

#pragma warning disable CS0618 // Type or member is obsolete
            services.TryAddEnumerable(ServiceDescriptor.Singleton<MetricExporter, SteeltoeExporter>());
            services.AddOpenTelemetry();

            services.TryAddSingleton((provider) => provider.GetServices<MetricExporter>().OfType<SteeltoeExporter>().SingleOrDefault());
#pragma warning restore CS0618 // Type or member is obsolete
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

            services.AddPrometheusActuatorServices(config);

            AddMetricsObservers(services, observerOptions);
#pragma warning disable CS0618 // Type or member is obsolete
            services.TryAddEnumerable(ServiceDescriptor.Singleton<MetricExporter, PrometheusExporter>());
            services.AddOpenTelemetry();
            services.TryAddSingleton((provider) => provider.GetServices<MetricExporter>().OfType<PrometheusExporter>().SingleOrDefault());
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddActuatorEndpointMapping<PrometheusScraperEndpoint>();
        }

        private static void AddMetricsObservers(IServiceCollection services, MetricsObserverOptions observerOptions)
        {
#pragma warning disable CS0618 // Type or member is obsolete
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

            if (observerOptions.GCEvents)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, GCEventsListener>());
            }

            if (observerOptions.EventCounterEvents)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, EventCounterListener>());
            }

            if (observerOptions.ThreadPoolEvents)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, ThreadPoolEventsListener>());
            }

            if (observerOptions.HystrixEvents)
            {
                services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, HystrixEventsListener>());
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static void AddOpenTelemetry(this IServiceCollection services)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            services.TryAddSingleton<MultiExporter>();
            services.TryAddSingleton((provider) =>
            {
                var exporter = provider.GetService<MultiExporter>();
                return new SteeltoeProcessor(exporter); // TODO: Capture from options when OTel Configuration is finalized
            });
            services.TryAddSingleton<IStats>((provider) =>
            {
                var processor = provider.GetService<SteeltoeProcessor>();
                return new OpenTelemetryMetrics(processor, TimeSpan.FromSeconds(3));
            });
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
