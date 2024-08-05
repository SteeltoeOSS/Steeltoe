// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Management.Endpoint.Environment;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds components of the Environment actuator to the D/I container.
    /// </summary>
    /// <param name="services">
    /// Service collection to add actuator to.
    /// </param>
    public static IServiceCollection AddEnvironmentActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IHostEnvironment>(provider => provider.GetRequiredService<IWebHostEnvironment>());

        services.AddCommonActuatorServices();
        services.AddEnvironmentActuatorServices();
        return services;
    }
}
