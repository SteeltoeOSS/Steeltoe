// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Diagnostics;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Wavefront.Exporters;

namespace Steeltoe.Management.Wavefront;

public static class WavefrontExtensions
{
    /// <summary>
    /// Adds the services used by the Wavefront exporter.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddWavefrontMetrics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.AddHostedService<DiagnosticsService>();

        services.ConfigureOptionsWithChangeTokenSource<WavefrontExporterOptions, ConfigureWavefrontExporterOptions>();
        services.AddMetricsObservers();

        services.AddOpenTelemetry().WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            builder.AddWavefrontExporter();
        });

        return services;
    }

    public static MeterProviderBuilder AddWavefrontExporter(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddReader(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<WavefrontMetricsExporter>>();
            var options = serviceProvider.GetRequiredService<IOptions<WavefrontExporterOptions>>();
            var wavefrontExporter = new WavefrontMetricsExporter(options.Value, logger);

            var metricReader = new PeriodicExportingMetricReader(wavefrontExporter, wavefrontExporter.Options.Step)
            {
                TemporalityPreference = MetricReaderTemporalityPreference.Cumulative
            };

            return metricReader;
        });
    }
}
