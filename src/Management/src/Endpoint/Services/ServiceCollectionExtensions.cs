// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Refresh;

namespace Steeltoe.Management.Endpoint.Services;

/// <summary>
/// Add services used by the Refresh actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Refresh actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddServicesActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureEndpointOptions<ServicesEndpointOptions, ConfigureServicesEndpointOptions>();
        services.AddSingleton(services);
        services.TryAddSingleton<IServicesEndpointHandler, ServicesEndpointHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, ServicesEndpointMiddleware>());
        services.AddSingleton<ServicesEndpointMiddleware>();

        return services;
    }
}
