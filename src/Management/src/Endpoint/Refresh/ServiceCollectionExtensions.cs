// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Refresh;

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
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddRefreshActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.ConfigureEndpointOptions<RefreshEndpointOptions, ConfigureRefreshEndpointOptions>();
        services.TryAddSingleton<IRefreshEndpoint, RefreshEndpoint>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, RefreshEndpointMiddleware>());
        services.AddSingleton<RefreshEndpointMiddleware>();


        return services;
    }
}
