// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Actuators.Refresh;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the refresh actuator to the service container and configures the ASP.NET middleware pipeline.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRefreshActuator(this IServiceCollection services)
    {
        return AddRefreshActuator(services, true);
    }

    /// <summary>
    /// Adds the refresh actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configureMiddleware">
    /// When <c>false</c>, skips configuration of the ASP.NET middleware pipeline. While this provides full control over the pipeline order, it requires to
    /// manually add the appropriate middleware for actuators to work correctly.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRefreshActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCoreActuatorServices<RefreshEndpointOptions, ConfigureRefreshEndpointOptions, RefreshEndpointMiddleware, IRefreshEndpointHandler,
            RefreshEndpointHandler, object?, IList<string>>(configureMiddleware);

        return services;
    }
}
