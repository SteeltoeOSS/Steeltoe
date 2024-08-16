// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the route mappings actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMappingsActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCommonActuatorServices();
        services.AddMappingsActuatorServices();

        services.AddEndpointsApiExplorer();
        services.TryAddSingleton<RouterMappings>();

        return services;
    }
}
