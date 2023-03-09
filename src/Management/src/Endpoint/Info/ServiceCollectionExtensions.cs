// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Info;

/// <summary>
/// Add services used by the Info actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Info actuator.
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
    public static IServiceCollection AddInfoActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);


        services.ConfigureEndpointOptions<InfoEndpointOptions, ConfigureInfoEndpointOptions>();
        services.TryAddSingleton<IInfoEndpoint,InfoEndpoint>();

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IEndpointMiddleware, InfoEndpointMiddleware>());
        services.AddScoped<InfoEndpointMiddleware>();

        return services;
    }
}
