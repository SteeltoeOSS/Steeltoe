// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Env;

/// <summary>
/// Add services used by the Env actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Env actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddEnvActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);
        services.ConfigureEndpointOptions<EnvEndpointOptions, ConfigureEnvEndpointOptions>();
        services.TryAddSingleton<EnvironmentEndpoint>();
        services.TryAddSingleton<IEnvironmentEndpoint>(provider => provider.GetRequiredService<EnvironmentEndpoint>());

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, EnvironmentEndpointMiddleware>());
        services.AddSingleton<EnvironmentEndpointMiddleware>();

        return services;
    }
}
