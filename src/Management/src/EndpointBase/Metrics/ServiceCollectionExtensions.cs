// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Add services used by the Metrics actuator
    /// </summary>
    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the services used by the Metrics actuator
        /// </summary>
        /// <param name="services">Reference to the service collection</param>
        /// <param name="configuration">Reference to the configuration system</param>
        /// <returns>A reference to the service collection</returns>
        public static IServiceCollection AddMetricsActuatorServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var options = new MetricsEndpointOptions(configuration);
            services.TryAddSingleton<IMetricsEndpointOptions>(options);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
            services.TryAddSingleton<MetricsEndpoint>();
            services.AddSingleton(new SteeltoeExporterOptions());

            services.TryAddSingleton<IMetricsEndpoint>(provider => provider.GetRequiredService<MetricsEndpoint>());
            services.TryAddSingleton(provider =>
            {
                var meterProvider = provider.AddOtelMetrics(out var steeltoeExporter, out _);
                services.TryAddSingleton(meterProvider);
                return steeltoeExporter;
            });
            return services;
        }

        /// <summary>
        /// Adds the services used by the Prometheus actuator
        /// </summary>
        /// <param name="services">Reference to the service collection</param>
        /// <param name="configuration">Reference to the configuration system</param>
        /// <returns>A reference to the service collection</returns>
        public static IServiceCollection AddPrometheusActuatorServices(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var options = new PrometheusEndpointOptions(configuration);
            services.TryAddSingleton<IPrometheusEndpointOptions>(options);
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
            services.TryAddSingleton<PrometheusScraperEndpoint>();
            services.TryAddSingleton<IPrometheusScraperEndpoint>(provider => provider.GetRequiredService<PrometheusScraperEndpoint>());
            services.TryAddSingleton(provider =>
            {
                var meterProvider = provider.AddOtelMetrics(out _, out var promExporter);
                services.TryAddSingleton(meterProvider);
                return promExporter;
            });
            return services;
        }

        private static MeterProvider AddOtelMetrics(this IServiceProvider provider, out SteeltoeExporter exporter, out PrometheusExporterWrapper promExporter)
        {
            var views = provider.GetService<IViewRegistry>();
            exporter = null;
            promExporter = null;
            var exporterOptions = provider.GetService<SteeltoeExporterOptions>();
            if (exporterOptions != null)
            {
                exporter = new SteeltoeExporter(exporterOptions);
            }

            if (provider.GetService<PrometheusEndpointOptions>() != null)
            {
                promExporter = new PrometheusExporterWrapper();
            }

            return OpenTelemetryMetrics.Initialize(views, exporter, promExporter);
        }

    }
}
