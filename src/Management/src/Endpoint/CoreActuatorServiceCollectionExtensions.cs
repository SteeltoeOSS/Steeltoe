// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.ManagementPort;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint;

public static class CoreActuatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers endpoint options configuration, middleware, and handler as singleton services.
    /// <para>
    /// This low-level extension method is intended to be called when implementing custom actuator endpoints.
    /// </para>
    /// </summary>
    /// <typeparam name="TEndpointOptions">
    /// The actuator-specific <see cref="EndpointOptions" /> to configure.
    /// </typeparam>
    /// <typeparam name="TConfigureEndpointOptions">
    /// The actuator-specific <see cref="EndpointOptions" /> configurer.
    /// </typeparam>
    /// <typeparam name="TMiddleware">
    /// The actuator-specific <see cref="IEndpointMiddleware" />.
    /// </typeparam>
    /// <typeparam name="TEndpointHandlerInterface">
    /// The actuator-specific <see cref="IEndpointHandler{TRequest, TResponse}" /> interface.
    /// </typeparam>
    /// <typeparam name="TEndpointHandler">
    /// The actuator-specific <see cref="IEndpointHandler{TRequest, TResponse}" /> implementation.
    /// </typeparam>
    /// <typeparam name="TRequest">
    /// The actuator-specific endpoint handler input type.
    /// </typeparam>
    /// <typeparam name="TResponse">
    /// The actuator-specific endpoint handler output type.
    /// </typeparam>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET Core middleware pipeline. While this provides full control over the pipeline order, it requires
    /// manual addition of the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddCoreActuatorServices<TEndpointOptions, TConfigureEndpointOptions, TMiddleware, TEndpointHandlerInterface,
        TEndpointHandler, TRequest, TResponse>(this IServiceCollection services, bool configureMiddleware)
        where TEndpointOptions : EndpointOptions
        where TConfigureEndpointOptions : class, IConfigureOptionsWithKey<TEndpointOptions>
        where TMiddleware : class, IEndpointMiddleware
        where TEndpointHandlerInterface : class, IEndpointHandler<TRequest, TResponse>
        where TEndpointHandler : class, TEndpointHandlerInterface
    {
        ArgumentNullException.ThrowIfNull(services);

        AddCommonActuatorServices(services, configureMiddleware);

        services.ConfigureEndpointOptions<TEndpointOptions, TConfigureEndpointOptions>();
        services.TryAddSingleton<TEndpointHandlerInterface, TEndpointHandler>();
        services.TryAddSingleton<TMiddleware>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, TMiddleware>());

        return services;
    }

    private static void AddCommonActuatorServices(IServiceCollection services, bool configureMiddleware)
    {
#pragma warning disable S4792 // Configuring loggers is security-sensitive
        services.AddLogging();
#pragma warning restore S4792 // Configuring loggers is security-sensitive

        services.AddRouting();
        services.TryAddSingleton<ActuatorRouteMapper>();
        services.TryAddSingleton<ActuatorEndpointMapper>();

        services.AddCors();
        services.TryAddSingleton<IConfigureOptions<CorsOptions>, ConfigureActuatorsCorsPolicyOptions>();

        services.ConfigureOptionsWithChangeTokenSource<ManagementOptions, ConfigureManagementOptions>();

        if (configureMiddleware)
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient<IStartupFilter, ConfigureActuatorsMiddlewareStartupFilter>());
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, ManagementPortStartupFilter>());
    }

    internal static void ConfigureEndpointOptions<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : EndpointOptions
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointOptionsMonitorProvider, EndpointOptionsMonitorProvider<TOptions>>());
    }

    internal static void ConfigureOptionsWithChangeTokenSource<TOptions, TConfigureOptions>(this IServiceCollection services)
        where TOptions : class
        where TConfigureOptions : class, IConfigureOptionsWithKey<TOptions>
    {
        services.AddOptions();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<TOptions>, TConfigureOptions>());

        services.TryAddSingleton<IOptionsChangeTokenSource<TOptions>, ConfigurationChangeTokenSource<TOptions>>();
    }

    internal static void PostConfigureOptionsWithChangeTokenSource<TOptions, TPostConfigureOptions>(this IServiceCollection services)
        where TOptions : class
        where TPostConfigureOptions : class, IPostConfigureOptions<TOptions>
    {
        services.AddOptions();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IPostConfigureOptions<TOptions>, TPostConfigureOptions>());

        services.TryAddSingleton<IOptionsChangeTokenSource<TOptions>, ConfigurationChangeTokenSource<TOptions>>();
    }

    /// <summary>
    /// Configures an <see cref="Action{IEndpointConventionBuilder}" /> to customize the mapped actuator endpoints.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureEndpoints">
    /// Takes an <see cref="IEndpointConventionBuilder" /> to customize the mapped endpoints. Useful for tailoring auth requirements.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection ConfigureActuatorEndpoints(this IServiceCollection services, Action<IEndpointConventionBuilder> configureEndpoints)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureEndpoints);

        services.Configure<ActuatorConventionOptions>(options => options.ConfigureActions.Add(configureEndpoints));

        return services;
    }

    /// <summary>
    /// Configures an <see cref="Action{CorsPolicyBuilder}" /> to customize the Cross-Origin Resource Sharing (CORS) policy that applies to all actuator
    /// endpoints.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureCorsPolicy">
    /// Takes an <see cref="CorsPolicyBuilder" /> to customize the policy. Useful to restrict access.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection ConfigureActuatorsCorsPolicy(this IServiceCollection services, Action<CorsPolicyBuilder> configureCorsPolicy)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureCorsPolicy);

        services.Configure<ActuatorsCorsPolicyOptions>(options => options.ConfigureActions.Add(configureCorsPolicy));

        return services;
    }
}
