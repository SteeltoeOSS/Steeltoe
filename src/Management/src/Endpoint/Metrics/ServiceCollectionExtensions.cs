// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Metrics.SystemDiagnosticsMetrics;
using Steeltoe.Management.Endpoint.Middleware;

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
    /// <returns>
    /// A reference to the service collection.
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

            return new MetricsExporterOptions
            {
                CacheDurationMilliseconds = options.CacheDurationMilliseconds,
                MaxTimeSeries = options.MaxTimeSeries,
                MaxHistograms = options.MaxHistograms,
                IncludedMetrics = options.IncludedMetrics
            };
        });

        services.TryAddSingleton(provider =>
        {
            var exporterOptions = provider.GetRequiredService<MetricsExporterOptions>();
            return new MetricsExporter(exporterOptions);
        });

        services.AddSteeltoeCollector();

        return services;
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

            if (exporterOptions.IncludedMetrics != null)
            {
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
            }

            return aggregationManager;
        }).AddHostedService<MetricCollectionHostedService>();
    }
}
