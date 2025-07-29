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
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection;

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

    /// <summary>
    /// Helper method to configure opentelemetry metrics. Do not use in conjuction with Extension methods provided by OpenTelemetry.
    /// </summary>
    /// <param name="services">Reference to the service collection</param>
    /// <param name="configure">The Action to configure OpenTelemetry</param>
    /// <param name="name">Instrumentation Name </param>
    /// <param name="version">Instrumentation Version</param>
    /// <returns>A reference to the service collection </returns>
    public static IServiceCollection AddOpenTelemetryMetricsForSteeltoe(this IServiceCollection services, Action<IServiceProvider, MeterProviderBuilder> configure = null, string name = null, string version = null)
    {
        if (services.Any(sd => sd.ServiceType == typeof(MeterProvider)))
        {
            if (!services.Any(sd => sd.ImplementationInstance?.ToString() == "{ ConfiguredSteeltoeMetrics = True }"))
            {
                Console.WriteLine("Warning!! Make sure one of the extension methods that calls ConfigureSteeltoeMetrics is used to correctly configure metrics using OpenTelemetry for Steeltoe.");
            }

            return services; // Already Configured, get out of here
        }

        services.AddSingleton(new { ConfiguredSteeltoeMetrics = true });
        services.AddOpenTelemetry().WithMetrics();
        services.ConfigureOpenTelemetryMeterProvider((serviceProvider, meterProviderBuilder) =>
            ConfigureSteeltoeMetrics(meterProviderBuilder, serviceProvider, configure, name, version));

        return services;
    }

    /// <summary>
    /// Configures the <see cref="MeterProviderBuilder"></see> as an underlying Metrics processor and exporter for Steeltoe in actuators and exporters. />
    /// </summary>
    /// <param name="builder">MeterProviderBuilder</param>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="configure"> Configuration callback</param>
    /// <param name="name">Instrumentation Name</param>
    /// <param name="version">Instrumentation Version</param>
    /// <returns>Configured MeterProviderBuilder</returns>
    public static MeterProviderBuilder ConfigureSteeltoeMetrics(this MeterProviderBuilder builder, IServiceProvider serviceProvider, Action<IServiceProvider, MeterProviderBuilder> configure = null, string name = null, string version = null)
    {
        configure?.Invoke(serviceProvider, builder);

        var views = serviceProvider.GetService<IViewRegistry>();
        var exporters = serviceProvider.GetServices(typeof(IMetricsExporter)) as System.Collections.Generic.IEnumerable<IMetricsExporter>;

        builder
            .AddMeter(name ?? OpenTelemetryMetrics.InstrumentationName, version ?? OpenTelemetryMetrics.InstrumentationVersion)
            .AddRegisteredViews(views)
            .AddExporters(exporters);

        var wavefrontExporter = serviceProvider.GetService<WavefrontMetricsExporter>(); // Not an IMetricsExporter

        if (wavefrontExporter != null)
        {
            builder.AddWavefrontExporter(wavefrontExporter);
        }

        return builder;
    }
}
