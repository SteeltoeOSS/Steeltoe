// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint.Actuators.Metrics.Observers;
using Steeltoe.Management.Endpoint.Actuators.Metrics.SystemDiagnosticsMetrics;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the metrics actuator to the service container and configures the ASP.NET middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMetricsActuator(this IServiceCollection services)
    {
        return AddMetricsActuator(services, true);
    }

    /// <summary>
    /// Adds the metrics actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET middleware pipeline. While this provides full control over the pipeline order, it requires to
    /// manually add the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMetricsActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDiagnosticsManager();

        services
            .AddCoreActuatorServicesAsSingleton<MetricsEndpointOptions, ConfigureMetricsEndpointOptions, MetricsEndpointMiddleware, IMetricsEndpointHandler,
                MetricsEndpointHandler, MetricsRequest?, MetricsResponse?>(configureMiddleware);

        services.TryAddSingleton<MetricsExporter>();

        AddSteeltoeCollector(services);
        services.AddMetricsObservers();

        return services;
    }

    private static void AddSteeltoeCollector(IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
        {
            var exporter = serviceProvider.GetRequiredService<MetricsExporter>();

            MetricsEndpointOptions endpointOptions = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsEndpointOptions>>().CurrentValue;
            var logger = serviceProvider.GetRequiredService<ILogger<MetricsExporter>>();

            var aggregationManager = new AggregationManager(endpointOptions.MaxTimeSeries, endpointOptions.MaxHistograms, exporter.AddMetrics,
                (intervalStartTime, nextIntervalStartTime) => logger.LogTrace("Begin collection from {IntervalStartTime} to {NextIntervalStartTime}",
                    intervalStartTime, nextIntervalStartTime),
                (intervalStartTime, nextIntervalStartTime) => logger.LogTrace("End collection from {IntervalStartTime} to {NextIntervalStartTime}",
                    intervalStartTime, nextIntervalStartTime),
                instrument => logger.LogTrace("Begin measurements from {InstrumentName} for {MeterName}", instrument.Name, instrument.Meter.Name),
                instrument => logger.LogTrace("End measurements from {InstrumentName} for {MeterName}", instrument.Name, instrument.Meter.Name),
                instrument => logger.LogTrace("Instrument {InstrumentName} published for {MeterName}", instrument.Name, instrument.Meter.Name),
                () => logger.LogTrace("Steeltoe metrics collector started."), exception => logger.LogError(exception, "An error occurred while collecting"),
                () => logger.LogWarning("Cannot collect any more time series because the configured limit of {MaxTimeSeries} was reached",
                    endpointOptions.MaxTimeSeries),
                () => logger.LogWarning("Cannot collect any more Histograms because the configured limit of {MaxHistograms} was reached",
                    endpointOptions.MaxHistograms), exception => logger.LogError(exception, "An error occurred while collecting observable instruments"));

            exporter.SetCollect(aggregationManager.Collect);
            aggregationManager.Include(SteeltoeMetrics.InstrumentationName); // Default to Steeltoe Metrics

            foreach (string filter in endpointOptions.IncludedMetrics)
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

    /// <summary>
    /// Adds metrics observers to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMetricsObservers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureOptionsWithChangeTokenSource<MetricsObserverOptions, ConfigureMetricsObserverOptions>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, AspNetCoreHostingObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpClientObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRuntimeDiagnosticSource, ClrRuntimeObserver>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<EventListener, EventCounterListener>());

        return services;
    }
}
