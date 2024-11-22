// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Wavefront.Exporters;

namespace Steeltoe.Management.Wavefront;

public static class WavefrontExtensions
{
    /// <summary>
    /// Adds the services used by the Wavefront exporter.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddWavefrontMetrics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDiagnosticsManager();

        services.ConfigureOptionsWithChangeTokenSource<WavefrontExporterOptions, ConfigureWavefrontExporterOptions>();

        services.AddOpenTelemetry().WithMetrics(builder =>
        {
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
