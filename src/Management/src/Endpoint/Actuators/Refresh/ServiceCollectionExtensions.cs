// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Refresh;

/// <summary>
/// Add services used by the Refresh actuator.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Refresh actuator.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRefreshActuatorServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.ConfigureEndpointOptions<RefreshEndpointOptions, ConfigureRefreshEndpointOptions>();
        services.TryAddSingleton<IRefreshEndpointHandler, RefreshEndpointHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, RefreshEndpointMiddleware>());
        services.AddSingleton<RefreshEndpointMiddleware>();

        return services;
    }
}
