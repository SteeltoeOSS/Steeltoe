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

        services.TryAddSingleton(provider =>
        {
            var options = provider.GetService<IMetricsEndpointOptions>();

            var exporterOptions = new PullMetricsExporterOptions
            {
                ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds
            };

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
            var aggMan =  new AggregationManager(100, 100,
                    (instrument, stats) => { steeltoeExporter.AddMetrics(instrument, stats); },
                    (date1, date2) => { /*begin*/ },
                    (date1, date2) => { /*end*/ },
                    (instrument) => { /*begin instrument*/},
                    (instrument) => { /* end instrument */},
                    (instrument) => { /* instrument published */},
                    () => {  /* enumeration complete*/ });
            steeltoeExporter.Collect = aggMan.Collect;
            aggMan.Include(SteeltoeMetrics.InstrumentationName);
            return aggMan;
        }).AddHostedService<MetricCollectionHostedService>();
    }

}
