// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
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
using System.Linq;

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

            return services;
        }

        /// <summary>
        /// Helper method to configure opentelemetry metrics. Do not use in conjuction with Extension methods provided by Opentelemetry.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configure"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IServiceCollection AddOpenTelemetryMetricsForSteeltoe(this IServiceCollection services, Action<IServiceProvider, MeterProviderBuilder> configure = null, string name = null, string version = null)
        {
            if (services.Any(sd => sd.ServiceType == typeof(MeterProvider)))
            {
                throw new InvalidOperationException("OpenTelemetry has already been configured! Use the configure method provided by AddOpenTelemetryMetricsForSteeltoe to customize your metrics pipeline instead of configuring OpenTelemetry separately");
            }

            return services.AddOpenTelemetryMetrics(builder => builder.ConfigureSteeltoeMetrics());
        }

        public static MeterProviderBuilder ConfigureSteeltoeMetrics(this MeterProviderBuilder builder, Action<IServiceProvider, MeterProviderBuilder> configure = null, string name = null, string version = null)
        {
            if (configure != null)
            {
                builder.Configure(configure);
            }

            builder.Configure((provider, deferredBuilder) =>
            {
                var views = provider.GetService<IViewRegistry>();
                var exporters = provider.GetServices(typeof(IMetricsExporter)) as System.Collections.Generic.IEnumerable<IMetricsExporter>;
                var services = deferredBuilder.GetServices();
                if (services.Any(sd => sd.ImplementationType == typeof(MeterProviderBuilder)))
                {
                    Console.WriteLine("MeterProviderBuilder is here");
                }

                deferredBuilder
                    .AddMeter(name ?? OpenTelemetryMetrics.InstrumentationName, version ?? OpenTelemetryMetrics.InstrumentationVersion)
                    .AddRegisteredViews(views)
                    .AddExporters(exporters);

                var wavefrontExporter = provider.GetService<WavefrontMetricsExporter>(); // Not an IMetricsExporter

                if (wavefrontExporter != null)
                {
                    deferredBuilder.AddWavefrontExporter(wavefrontExporter);
                }
            });
            return builder;
        }
    }
}
