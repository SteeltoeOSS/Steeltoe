// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Metrics;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Prometheus;

public static class PrometheusExtensions
{
    /// <summary>
    /// Adds the services used by the Steeltoe-configured OpenTelemetry Prometheus exporter and configures the ASP.NET middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services)
    {
        return AddPrometheusActuator(services, true, null);
    }

    /// <summary>
    /// Adds the services used by the Steeltoe-configured OpenTelemetry Prometheus exporter.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for the Prometheus exporter to work correctly.
    /// </param>
    /// <param name="configureBranchedPipeline">
    /// Optional callback to configure the branched pipeline. Called before registration of the Prometheus middleware.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services, bool configureMiddleware,
        Action<IApplicationBuilder>? configureBranchedPipeline)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureEndpointOptions<PrometheusEndpointOptions, ConfigurePrometheusEndpointOptions>();

        if (configureMiddleware)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, ConfigurePrometheusActuatorStartupFilter>(_ =>
                new ConfigurePrometheusActuatorStartupFilter(configureBranchedPipeline)));
        }

        services.AddOpenTelemetry().WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            builder.AddPrometheusExporter();
        });

        return services;
    }

    /// <summary>
    /// Adds the Steeltoe-configured OpenTelemetry Prometheus exporter to an <see cref="IApplicationBuilder" /> instance.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UsePrometheusActuator(this IApplicationBuilder builder)
    {
        return UsePrometheusActuator(builder, null);
    }

    /// <summary>
    /// Adds the Steeltoe-configured OpenTelemetry Prometheus exporter to an <see cref="IApplicationBuilder" /> instance.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IApplicationBuilder" />.
    /// </param>
    /// <param name="configureBranchedPipeline">
    /// Optional callback to configure the branched pipeline. Called before registration of the Prometheus middleware.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UsePrometheusActuator(this IApplicationBuilder builder, Action<IApplicationBuilder>? configureBranchedPipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ManagementOptions managementOptions = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<ManagementOptions>>().CurrentValue;
        var conventionOptions = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<ActuatorConventionOptions>>();

        PrometheusEndpointOptions? prometheusOptions = builder.ApplicationServices.GetServices<EndpointOptions>()
            .OfType<PrometheusEndpointOptions>().FirstOrDefault();

        string basePath = managementOptions.Path ?? "/actuator";
        string endpointPath = prometheusOptions?.Path ?? "prometheus";
        string path = $"{basePath}/{endpointPath}".Replace("//", "/", StringComparison.Ordinal);

        builder.UseOpenTelemetryPrometheusScrapingEndpoint(null, null, path, ConfigureBranchedPipeline, null);

        if (Platform.IsCloudFoundry)
        {
            var permissionsProvider = builder.ApplicationServices.GetService<PermissionsProvider>();

            if (permissionsProvider is null)
            {
                var loggerFactory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
                ILogger logger = loggerFactory.CreateLogger(nameof(PrometheusExtensions));
                logger.LogWarning("The Cloud Foundry Actuator is required in order to run the Prometheus exporter under the Cloud Foundry context.");
                return builder;
            }

            string cloudFoundryPath = $"/{ConfigureManagementOptions.DefaultCloudFoundryPath}/{endpointPath}".Replace("//", "/", StringComparison.Ordinal);
            builder.UseOpenTelemetryPrometheusScrapingEndpoint(null, null, cloudFoundryPath, ConfigureBranchedPipeline, null);
        }

        return builder;

        void ConfigureBranchedPipeline(IApplicationBuilder branchedApplicationBuilder)
        {
            branchedApplicationBuilder.UseRouting();
            configureBranchedPipeline?.Invoke(branchedApplicationBuilder);

            branchedApplicationBuilder.UseEndpoints(endpoints =>
            {
                IEndpointConventionBuilder endpointBuilder = endpoints.MapPrometheusScrapingEndpoint("/");

                foreach (Action<IEndpointConventionBuilder> endpointAction in conventionOptions.CurrentValue.ConfigureActions)
                {
                    endpointAction(endpointBuilder);
                }
            });
        }
    }
}
