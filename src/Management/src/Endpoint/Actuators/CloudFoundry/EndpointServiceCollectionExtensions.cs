// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Cloud Foundry actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddCloudFoundryActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddCoreActuatorServicesAsSingleton<CloudFoundryEndpointOptions, ConfigureCloudFoundryEndpointOptions, CloudFoundryEndpointMiddleware,
                ICloudFoundryEndpointHandler, CloudFoundryEndpointHandler, string, Links>();

        return services;
    }
}
