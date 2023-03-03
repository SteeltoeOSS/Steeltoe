// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Env;
using Steeltoe.Management.Endpoint.HeapDump;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Hypermedia;

/// <summary>
/// Add services used by the Hypermedia actuator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Hypermedia actuator.
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
    public static IServiceCollection AddHypermediaActuatorServices(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);
        // ArgumentGuard.NotNull(configuration);

        //var options = new HypermediaEndpointOptions(configuration);
        //services.TryAddSingleton<IActuatorHypermediaOptions>(options);
        //services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));

        // services.ConfigureOptions<ConfigureHypermediaEndpointOptions>();

        services.ConfigureEndpointOptions<HypermediaEndpointOptions, ConfigureHypermediaEndpointOptions>();
        services.TryAddSingleton<ActuatorEndpoint>();
        services.TryAddSingleton<IActuatorEndpoint>(provider => provider.GetRequiredService<ActuatorEndpoint>());

        // New:
        //services.AddSingleton<IEndpointMiddleware, ActuatorHypermediaEndpointMiddleware>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IEndpointMiddleware, ActuatorHypermediaEndpointMiddleware>());
        services.AddSingleton<ActuatorHypermediaEndpointMiddleware>();

        return services;
    }
}
