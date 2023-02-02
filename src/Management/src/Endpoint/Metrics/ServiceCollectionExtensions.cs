// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Logging;
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Exporters;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;

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
        var exporterOptions = new MetricsExporterOptions
        {
            CacheDurationMilliseconds = options.CacheDurationMilliseconds,
            MaxTimeSeries = options.MaxTimeSeries,
            MaxHistograms = options.MaxHistograms,
            IncludedMetrics = options.IncludedMetrics
        };
        services.TryAddSingleton<IExporterOptions>(exporterOptions);
        services.TryAddSingleton((provider)=>
        {
            var exporterOptions = provider.GetService<IExporterOptions>();
            return new SteeltoeExporter(exporterOptions);
        });
        services.AddSteeltoeCollector();

        return services;
    }

    public static IServiceCollection AddSteeltoeCollector(this IServiceCollection services)
    {
       return services.AddSingleton((provider) =>
        {
            var steeltoeExporter = provider.GetService<SteeltoeExporter>();
            var exporterOptions = provider.GetService<IExporterOptions>();
            var logger = provider.GetService<ILogger<SteeltoeExporter>>();
            var aggregationManager = new AggregationManager(
                maxTimeSeries: exporterOptions.MaxTimeSeries,
                maxHistograms: exporterOptions.MaxHistograms,
                collectMeasurement: steeltoeExporter.AddMetrics,
                beginInstrumentMeasurements: (instrument) => logger.LogTrace($"Begin measurements from {instrument.Name} for {instrument.Meter.Name}"),
                endInstrumentMeasurements: (instrument) => logger.LogTrace($"End measurements from {instrument.Name} for {instrument.Meter.Name}"),
                instrumentPublished: (instrument) => logger.LogTrace($"Instrument {instrument.Name} published for {instrument.Meter.Name}"),
                initialInstrumentEnumerationComplete: () => logger.LogTrace("Steeltoe metrics collector started."),
                timeSeriesLimitReached: () => logger.LogWarning($"Cannnot collect any more time series because the configured limit of {exporterOptions.MaxTimeSeries} was reached"),
                histogramLimitReached: () => logger.LogWarning($"Cannnot collect any more Histograms because the configured limit of {exporterOptions.MaxHistograms} was reached"),
                observableInstrumentCallbackError: (ex) => logger.LogError(ex, "An error occured while collecting Observable Instruments "));

            steeltoeExporter.Collect = aggregationManager.Collect;
            aggregationManager.Include(SteeltoeMetrics.InstrumentationName); // Limit to Steeltoe Metrics; TODO: Configurable
            return aggregationManager;
        }).AddHostedService<MetricCollectionHostedService>();
    }

}