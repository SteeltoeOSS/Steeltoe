// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Actuators.Metrics.SystemDiagnosticsMetrics;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

/// <summary>
/// Add services used by the Metrics actuator.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Metrics actuator.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMetricsActuatorServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureEndpointOptions<MetricsEndpointOptions, ConfigureMetricsEndpointOptions>();
        services.TryAddSingleton<IMetricsEndpointHandler, MetricsEndpointHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, MetricsEndpointMiddleware>());
        services.TryAddSingleton<MetricsEndpointMiddleware>();

        services.TryAddSingleton(provider =>
        {
            MetricsEndpointOptions options = provider.GetRequiredService<IOptionsMonitor<MetricsEndpointOptions>>().CurrentValue;
            return CreateMetricsExporterOptionsFrom(options);
        });

        services.TryAddSingleton(provider =>
        {
            var exporterOptions = provider.GetRequiredService<MetricsExporterOptions>();
            return new MetricsExporter(exporterOptions);
        });

        services.AddSteeltoeCollector();

        return services;
    }

    private static MetricsExporterOptions CreateMetricsExporterOptionsFrom(MetricsEndpointOptions endpointOptions)
    {
        var exporterOptions = new MetricsExporterOptions
        {
            CacheDurationMilliseconds = endpointOptions.CacheDurationMilliseconds,
            MaxTimeSeries = endpointOptions.MaxTimeSeries,
            MaxHistograms = endpointOptions.MaxHistograms
        };

        foreach (string metric in endpointOptions.IncludedMetrics)
        {
            exporterOptions.IncludedMetrics.Add(metric);
        }

        return exporterOptions;
    }

    public static IServiceCollection AddSteeltoeCollector(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSingleton(provider =>
        {
            var exporter = provider.GetRequiredService<MetricsExporter>();

            var exporterOptions = provider.GetRequiredService<MetricsExporterOptions>();
            var logger = provider.GetRequiredService<ILogger<MetricsExporter>>();

            var aggregationManager = new AggregationManager(exporterOptions.MaxTimeSeries, exporterOptions.MaxHistograms, exporter.AddMetrics,
                (intervalStartTime, nextIntervalStartTime) => logger.LogTrace("Begin collection from {IntervalStartTime} to {NextIntervalStartTime}",
                    intervalStartTime, nextIntervalStartTime),
                (intervalStartTime, nextIntervalStartTime) => logger.LogTrace("End collection from {IntervalStartTime} to {NextIntervalStartTime}",
                    intervalStartTime, nextIntervalStartTime),
                instrument => logger.LogTrace("Begin measurements from {InstrumentName} for {MeterName}", instrument.Name, instrument.Meter.Name),
                instrument => logger.LogTrace("End measurements from {InstrumentName} for {MeterName}", instrument.Name, instrument.Meter.Name),
                instrument => logger.LogTrace("Instrument {InstrumentName} published for {MeterName}", instrument.Name, instrument.Meter.Name),
                () => logger.LogTrace("Steeltoe metrics collector started."), exception => logger.LogError(exception, "An error occurred while collecting"),
                () => logger.LogWarning("Cannot collect any more time series because the configured limit of {MaxTimeSeries} was reached",
                    exporterOptions.MaxTimeSeries),
                () => logger.LogWarning("Cannot collect any more Histograms because the configured limit of {MaxHistograms} was reached",
                    exporterOptions.MaxHistograms), exception => logger.LogError(exception, "An error occurred while collecting observable instruments"));

            exporter.SetCollect(aggregationManager.Collect);
            aggregationManager.Include(SteeltoeMetrics.InstrumentationName); // Default to Steeltoe Metrics

            foreach (string filter in exporterOptions.IncludedMetrics)
            {
                string[] filterParts = filter.Split(':');

                if (filterParts.Length == 2)
                {
                    string meter = filterParts[0];
                    string instrument = filterParts[1];
                    aggregationManager.Include(meter, instrument);
                }
            }

            return aggregationManager;
        }).AddHostedService<MetricCollectionHostedService>();
    }
}
