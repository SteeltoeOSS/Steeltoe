// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.MetricCollectors;

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

        services.AddOptions<PrometheusEndpointOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            configuration.GetSection(PrometheusEndpointOptions.ManagementInfoPrefix).Bind(options);
        });

        ServiceDescriptor sd = ServiceDescriptor.Singleton<IEndpointOptions, PrometheusEndpointOptions>();
        services.TryAddEnumerable(sd);

        services.AddOpenTelemetry().WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            builder.AddPrometheusExporter();
        }).StartWithHost();

        return services;
    }

    public static IApplicationBuilder MapPrometheusActuator(this IApplicationBuilder app)
    {
        ActuatorManagementOptions? managementOptions =
            app.ApplicationServices.GetService<IEnumerable<IManagementOptions>>()?.OfType<ActuatorManagementOptions>().FirstOrDefault();

        PrometheusEndpointOptions? prometheusOptions =
            app.ApplicationServices.GetService<IEnumerable<IEndpointOptions>>()?.OfType<PrometheusEndpointOptions>().FirstOrDefault();

        string root = managementOptions?.Path ?? "/actuator";
        string id = prometheusOptions?.Id ?? "prometheus";
        string path = root + "/" + id;

        return app.UseOpenTelemetryPrometheusScrapingEndpoint(path);
    }
}
