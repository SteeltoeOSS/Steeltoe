// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Prometheus;

public static class PrometheusExtensions
{
    /// <summary>
    /// Adds the services used by the Steeltoe-configured OpenTelemetry Prometheus exporter and configures the ASP.NET Core middleware pipeline.
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
    /// When <c>false</c>, skips configuration of the ASP.NET Core middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for the Prometheus exporter to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services, bool configureMiddleware)
    {
        return AddPrometheusActuator(services, configureMiddleware, null);
    }

    /// <summary>
    /// Adds the services used by the Steeltoe-configured OpenTelemetry Prometheus exporter.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET Core middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for the Prometheus exporter to work correctly.
    /// </param>
    /// <param name="configurePrometheusPipeline">
    /// Optional callback to run additional middleware at the Prometheus endpoint path, before the Prometheus middleware runs. For example:
    /// <code><![CDATA[builder => builder.UseAuthorization()]]></code>. Only used when <paramref name="configureMiddleware" /> is <c>true</c>.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services, bool configureMiddleware,
        Action<IApplicationBuilder>? configurePrometheusPipeline)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!configureMiddleware && configurePrometheusPipeline != null)
        {
            throw new InvalidOperationException($"The Prometheus pipeline cannot be configured here when {nameof(configureMiddleware)} is false.");
        }

        services.AddRouting();
        services.ConfigureEndpointOptions<PrometheusEndpointOptions, ConfigurePrometheusEndpointOptions>();
        services.ConfigureOptionsWithChangeTokenSource<ManagementOptions, ConfigureManagementOptions>();
        services.TryAddSingleton<HasCloudFoundrySecurityMiddlewareMarker>();

        if (configureMiddleware)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, PrometheusActuatorStartupFilter>(_ =>
                new PrometheusActuatorStartupFilter(configurePrometheusPipeline)));
        }

        services.AddOpenTelemetry().WithMetrics(builder => builder.AddPrometheusExporter());

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
    /// <param name="configurePrometheusPipeline">
    /// Optional callback to run additional middleware at the Prometheus endpoint path, before the Prometheus middleware runs. For example:
    /// <code><![CDATA[builder => builder.UseAuthorization()]]></code>.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IApplicationBuilder UsePrometheusActuator(this IApplicationBuilder builder, Action<IApplicationBuilder>? configurePrometheusPipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var loggerFactory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(nameof(PrometheusExtensions));
        ManagementOptions managementOptions = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<ManagementOptions>>().CurrentValue;
        var conventionOptionsMonitor = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<ActuatorConventionOptions>>();
        PrometheusEndpointOptions prometheusOptions = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<PrometheusEndpointOptions>>().CurrentValue;

        string endpointPath = prometheusOptions.GetEndpointPath(managementOptions.Path);
        string? cloudFoundryPath = null;

        if (Platform.IsCloudFoundry)
        {
            var permissionsProvider = builder.ApplicationServices.GetService<PermissionsProvider>();

            if (permissionsProvider is null)
            {
                logger.LogWarning("The Cloud Foundry Actuator is required in order to run the Prometheus exporter under the Cloud Foundry context.");
            }
            else
            {
                cloudFoundryPath = prometheusOptions.GetEndpointPath(ConfigureManagementOptions.DefaultCloudFoundryPath);
            }
        }

        var mvcOptions = builder.ApplicationServices.GetService<IOptions<MvcOptions>>();
        bool isEndpointRoutingEnabled = mvcOptions?.Value.EnableEndpointRouting ?? true;
        bool applyActuatorConventions = conventionOptionsMonitor.CurrentValue.ConfigureActions.Count > 0;

        if (applyActuatorConventions && !isEndpointRoutingEnabled)
        {
            logger.LogWarning("Customizing endpoints is only supported when using endpoint routing.");
        }

        if (managementOptions.Port == 0 && !applyActuatorConventions && configurePrometheusPipeline is null)
        {
            logger.LogWarning(
                "The Prometheus endpoint may not be configured securely. Consider using a dedicated management port, adding actuator conventions or configuring the Prometheus middleware pipeline.");
        }

        builder.UseOpenTelemetryPrometheusScrapingEndpoint(null, null, endpointPath, ConfigureBranchedPipeline, null);

        if (cloudFoundryPath != null)
        {
            builder.UseOpenTelemetryPrometheusScrapingEndpoint(null, null, cloudFoundryPath, ConfigureBranchedPipeline, null);
        }

        return builder;

        void ConfigureBranchedPipeline(IApplicationBuilder branchedApplicationBuilder)
        {
            if (isEndpointRoutingEnabled && applyActuatorConventions)
            {
                branchedApplicationBuilder.UseRouting();
            }

            configurePrometheusPipeline?.Invoke(branchedApplicationBuilder);

            if (isEndpointRoutingEnabled && applyActuatorConventions)
            {
                branchedApplicationBuilder.UseEndpoints(endpoints =>
                {
                    IEndpointConventionBuilder endpointBuilder = endpoints.MapPrometheusScrapingEndpoint("/");

                    foreach (Action<IEndpointConventionBuilder> endpointAction in conventionOptionsMonitor.CurrentValue.ConfigureActions)
                    {
                        endpointAction(endpointBuilder);
                    }
                });
            }
        }
    }
}
