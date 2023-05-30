// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.CloudFoundry;

/// <summary>
/// Add services used by the CloudFoundry actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Cloud Foundry actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddCloudFoundryActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);
        services.AddCommonActuatorServices();

        services.ConfigureEndpointOptions<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, CloudFoundryEndpointMiddleware>());
        services.AddSingleton<CloudFoundryEndpointMiddleware>();
        services.TryAddSingleton<ICloudFoundryEndpointHandler, CloudFoundryEndpointHandler>();
        return services;
    }
}
