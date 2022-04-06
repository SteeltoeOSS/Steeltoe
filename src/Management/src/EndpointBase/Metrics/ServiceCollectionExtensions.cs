// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Diagnostics;

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

            services.TryAddSingleton<IMetricsEndpoint>(provider => provider.GetRequiredService<MetricsEndpoint>());

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricsExporter, SteeltoeExporter>(provider =>
            {
                var options = provider.GetService<IMetricsEndpointOptions>();
                var exporterOptions = new PullmetricsExporterOptions() { ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds };
                return new SteeltoeExporter(exporterOptions);
            }));

            services.AddOpenTelemetryMetricsForSteeltoe();
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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricsExporter, SteeltoePrometheusExporter>(provider =>
            {
                var options = provider.GetService<IMetricsEndpointOptions>();
                var exporterOptions = new PullmetricsExporterOptions() { ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds };
                return new SteeltoePrometheusExporter(exporterOptions);
            }));
            services.AddOpenTelemetryMetricsForSteeltoe();

            return services;
        }

        public static IServiceCollection AddOpenTelemetryMetricsForSteeltoe(this IServiceCollection services, string name = null, string version = null)
        {
            return services.AddOpenTelemetryMetrics(builder =>
            {
                builder.Configure((provider, deferredBuilder) =>
                {
                    var views = provider.GetService<IViewRegistry>();
                    var exporters = provider.GetServices(typeof(IMetricsExporter)) as System.Collections.Generic.IEnumerable<IMetricsExporter>;
                    deferredBuilder
                        .AddMeter(name ?? OpenTelemetryMetrics.InstrumentationName, version ?? OpenTelemetryMetrics.InstrumentationVersion)
                        .AddRegisteredViews(views)
                        .AddExporters(exporters);

                    var wavefrontExporter = provider.GetServices(typeof(WavefrontMetricsExporter)) as WavefrontMetricsExporter;
                    if (wavefrontExporter != null)
                    {
                        deferredBuilder.AddWavefrontExporter(wavefrontExporter);
                    }
                });
            });
        }
    }
}
