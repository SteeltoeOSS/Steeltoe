// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Prometheus;
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

            services.AddSingleton(provider =>
            {
                services.AddSingleton<PrometheusExporterWrapper>();
                var options = provider.GetService<SteeltoeExporterOptions>();
                var steeltoeExporter = new SteeltoeExporter(options);
                var promExporter = provider.GetService<PrometheusExporterWrapper>();
                OpenTelemetryMetrics.Initialize(steeltoeExporter, promExporter);
                return steeltoeExporter;
            });
            services.TryAddSingleton<IMetricsEndpoint>(provider => provider.GetRequiredService<MetricsEndpoint>());

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

            return services;
        }
    }
}
