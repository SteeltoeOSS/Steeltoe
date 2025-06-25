// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the HTTP exchanges actuator to the service container and configures the ASP.NET Core middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddHttpExchangesActuator(this IServiceCollection services)
    {
        return AddHttpExchangesActuator(services, true);
    }

    /// <summary>
    /// Adds the HTTP exchanges actuator to the service container.
    /// </summary>
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
    public static IServiceCollection AddHttpExchangesActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCoreActuatorServices<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions, HttpExchangesEndpointMiddleware,
            IHttpExchangesEndpointHandler, HttpExchangesEndpointHandler, object?, HttpExchangesResult>(configureMiddleware);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<DiagnosticsManager>();
        services.AddHostedService<DiagnosticsService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticObserver, DiagnosticObserverHttpExchangeRecorder>());

        services.TryAddSingleton<IHttpExchangeRecorder>(serviceProvider =>
            serviceProvider.GetServices<DiagnosticObserver>().OfType<DiagnosticObserverHttpExchangeRecorder>().Single());

        services.TryAddSingleton<HttpExchangesRepository>();

        return services;
    }
}
