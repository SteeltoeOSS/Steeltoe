// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Prometheus;

public static class PrometheusExtensions
{
    /// <summary>
    /// Adds the services used by the Prometheus actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureEndpointOptions<PrometheusEndpointOptions, ConfigurePrometheusEndpointOptions>();

        services.AddOpenTelemetry().WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            builder.AddPrometheusExporter();
        });

        return services;
    }

    public static IApplicationBuilder MapPrometheusActuator(this IApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        ManagementOptions? managementOptions = builder.ApplicationServices.GetService<IOptionsMonitor<ManagementOptions>>()?.CurrentValue;

        PrometheusEndpointOptions? prometheusOptions = builder.ApplicationServices.GetService<IEnumerable<EndpointOptions>>()
            ?.OfType<PrometheusEndpointOptions>().FirstOrDefault();

        string root = managementOptions?.Path ?? "/actuator";
        string id = prometheusOptions?.Id ?? "prometheus";
        string path = root + "/" + id;

        return builder.UseOpenTelemetryPrometheusScrapingEndpoint(path);
    }
}
