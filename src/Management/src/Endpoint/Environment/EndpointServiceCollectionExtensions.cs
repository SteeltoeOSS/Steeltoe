// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;

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
        ArgumentGuard.NotNull(services);

        services.TryAddSingleton<IHostEnvironment>(provider =>
        {
            var service = provider.GetRequiredService<IWebHostEnvironment>();

            return new GenericHostingEnvironment
            {
                EnvironmentName = service.EnvironmentName,
                ApplicationName = service.ApplicationName,
                ContentRootFileProvider = service.ContentRootFileProvider,
                ContentRootPath = service.ContentRootPath
            };
        });

        services.AddCommonActuatorServices();
        services.AddEnvironmentActuatorServices();
        return services;
    }
}
