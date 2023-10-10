// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
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
        ArgumentGuard.NotNull(services);

        services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticsService>());

        services.ConfigureOptionsWithChangeTokenSource<WavefrontExporterOptions, ConfigureWavefrontExporterOptions>();
        services.AddMetricsObservers();

        services.AddOpenTelemetry().WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            builder.AddWavefrontExporter();
        }).StartWithHost();

        return services;
    }

    /// <summary>
    /// Add wavefront metrics to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your Hostbuilder.
    /// </param>
    /// <returns>
    /// The updated HostBuilder.
    /// </returns>
    public static IHostBuilder AddWavefrontMetrics(this IHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, collection) =>
        {
            collection.AddWavefrontMetrics();
        });
    }

    /// <summary>
    /// Add Wavefront Metrics Exporter.
    /// </summary>
    /// <param name="applicationBuilder">
    /// Your <see cref="WebApplicationBuilder" />.
    /// </param>
    public static WebApplicationBuilder AddWavefrontMetrics(this WebApplicationBuilder applicationBuilder)
    {
        ArgumentGuard.NotNull(applicationBuilder);

        applicationBuilder.Services.AddWavefrontMetrics();
        return applicationBuilder;
    }

    /// <summary>
    /// Adds Wavefront to the application.
    /// </summary>
    /// <param name="hostBuilder">
    /// Your HostBuilder.
    /// </param>
    public static IWebHostBuilder AddWavefrontMetrics(this IWebHostBuilder hostBuilder)
    {
        ArgumentGuard.NotNull(hostBuilder);

        return hostBuilder.ConfigureServices((_, collection) => collection.AddWavefrontMetrics());
    }

    public static MeterProviderBuilder AddWavefrontExporter(this MeterProviderBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

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
