// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Services;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Services actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add actuator to.
    /// </param>
    public static IServiceCollection AddServicesActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddCommonActuatorServices();
        services.AddServicesActuatorServices();

        return services;
    }
}
