// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the service-registry actuator to the service container and configures the ASP.NET Core middleware pipeline. This actuator gets or changes the
    /// status of this app in the discovery server (Eureka or Consul) that it is registered with, for example to take it out of service during a blue/green
    /// deployment.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddServiceRegistryActuator(this IServiceCollection services)
    {
        return AddServiceRegistryActuator(services, true);
    }

    /// <summary>
    /// Adds the service-registry actuator to the service container. This actuator gets or changes the status of this app in the discovery server (Eureka or
    /// Consul) that it is registered with, for example to take it out of service during a blue/green deployment.
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
    public static IServiceCollection AddServiceRegistryActuator(this IServiceCollection services, bool configureMiddleware)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCoreActuatorServices<ServiceRegistryEndpointOptions, ConfigureServiceRegistryEndpointOptions, ServiceRegistryEndpointMiddleware,
            IServiceRegistryEndpointHandler, ServiceRegistryEndpointHandler, ServiceRegistryRequest, ServiceRegistryResponse>(configureMiddleware);

        return services;
    }
}
