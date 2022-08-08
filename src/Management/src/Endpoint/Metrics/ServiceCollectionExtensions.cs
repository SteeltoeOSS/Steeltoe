// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics;

/// <summary>
/// Add services used by the Metrics actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Metrics actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddMetricsActuatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var options = new MetricsEndpointOptions(configuration);
        services.TryAddSingleton<IMetricsEndpointOptions>(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
        services.TryAddSingleton<MetricsEndpoint>();

        services.TryAddSingleton<IMetricsEndpoint>(provider => provider.GetRequiredService<MetricsEndpoint>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<MetricsExporter, SteeltoeExporter>(provider =>
        {
            var options = provider.GetService<IMetricsEndpointOptions>();

            var exporterOptions = new PullMetricsExporterOptions
            {
                ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds
            };

            return new SteeltoeExporter(exporterOptions);
        }));

        services.AddOpenTelemetryMetricsForSteeltoe();

        return services;
    }

    /// <summary>
    /// Adds the services used by the Prometheus actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddPrometheusActuatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var options = new PrometheusEndpointOptions(configuration);
        services.TryAddSingleton<IPrometheusEndpointOptions>(options);
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
        services.TryAddSingleton<PrometheusScraperEndpoint>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<MetricsExporter, SteeltoePrometheusExporter>(provider =>
        {
            var options = provider.GetService<IMetricsEndpointOptions>();

            var exporterOptions = new PullMetricsExporterOptions
            {
                ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds
            };

            return new SteeltoePrometheusExporter(exporterOptions);
        }));

        services.AddOpenTelemetryMetricsForSteeltoe();

        return services;
    }

    /// <summary>
    /// Helper method to configure opentelemetry metrics. Do not use in conjunction with Extension methods provided by OpenTelemetry.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configure">
    /// The Action to configure OpenTelemetry.
    /// </param>
    /// <param name="name">
    /// Instrumentation Name.
    /// </param>
    /// <param name="version">
    /// Instrumentation Version.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddOpenTelemetryMetricsForSteeltoe(this IServiceCollection services,
        Action<IServiceProvider, MeterProviderBuilder> configure = null, string name = null, string version = null)
    {
        if (services.Any(sd => sd.ServiceType == typeof(MeterProvider)))
        {
            if (!services.Any(sd => sd.ImplementationInstance?.ToString() == "{ ConfiguredSteeltoeMetrics = True }"))
            {
                Console.WriteLine(
                    "Warning!! Make sure one of the extension methods that calls ConfigureSteeltoeMetrics is used to correctly configure metrics using OpenTelemetry for Steeltoe.");
            }

            return services; // Already Configured, get out of here
        }

        services.AddSingleton(new
        {
            ConfiguredSteeltoeMetrics = true
        });

        return services.AddOpenTelemetryMetrics(builder => builder.ConfigureSteeltoeMetrics());
    }

    /// <summary>
    /// Configures the <see cref="MeterProviderBuilder"></see> as an underlying Metrics processor and exporter for Steeltoe in actuators and exporters. />.
    /// </summary>
    /// <param name="builder">
    /// MeterProviderBuilder.
    /// </param>
    /// <param name="configure">
    /// Configuration callback.
    /// </param>
    /// <param name="name">
    /// Instrumentation Name.
    /// </param>
    /// <param name="version">
    /// Instrumentation Version.
    /// </param>
    /// <returns>
    /// Configured MeterProviderBuilder.
    /// </returns>
    public static MeterProviderBuilder ConfigureSteeltoeMetrics(this MeterProviderBuilder builder,
        Action<IServiceProvider, MeterProviderBuilder> configure = null, string name = null, string version = null)
    {
        if (configure != null)
        {
            builder.Configure(configure);
        }

        builder.Configure((provider, deferredBuilder) =>
        {
            var views = provider.GetService<IViewRegistry>();
            var exporters = provider.GetServices(typeof(MetricsExporter)) as IEnumerable<MetricsExporter>;

            deferredBuilder.AddMeter(name ?? OpenTelemetryMetrics.InstrumentationName, version ?? OpenTelemetryMetrics.InstrumentationVersion)
                .AddRegisteredViews(views).AddExporters(exporters);

            var wavefrontExporter = provider.GetService<WavefrontMetricsExporter>(); // Not an IMetricsExporter

            if (wavefrontExporter != null)
            {
                deferredBuilder.AddWavefrontExporter(wavefrontExporter);
            }
        });

        return builder;
    }
}
