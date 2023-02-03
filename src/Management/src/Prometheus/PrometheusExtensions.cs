using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics.Prometheus;
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
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services
            .AddOptions<PrometheusEndpointOptions>()
            .Configure<IConfiguration>((options, Configuration) =>
            {
                Configuration.GetSection(PrometheusEndpointOptions.ManagementInfoPrefix).Bind(options);
            });

        var sd = ServiceDescriptor.Singleton<IEndpointOptions, PrometheusEndpointOptions>();
        services.TryAddEnumerable(sd);

        services.AddOpenTelemetry()
        .WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            builder.AddPrometheusExporter();
        })
        .StartWithHost();

        return services;
    }
    public static IApplicationBuilder MapPrometheusActuator(
          this IApplicationBuilder app)
    {
        var managementOptions = app.ApplicationServices.GetService<IEnumerable<IManagementOptions>>()?.OfType<ActuatorManagementOptions>().FirstOrDefault();
        var prometheusOptions = app.ApplicationServices.GetService<IEnumerable<IEndpointOptions>>()?.OfType<PrometheusEndpointOptions>().FirstOrDefault();

        var root = managementOptions?.Path ?? "/actuator";
        var id = prometheusOptions?.Id ?? "prometheus";
        var path = root + "/" + id;

        return app.UseOpenTelemetryPrometheusScrapingEndpoint(path);
    }
}
