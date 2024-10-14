// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Services;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddServicesActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(services);

        services
            .AddCoreActuatorServicesAsSingleton<ServicesEndpointOptions, ConfigureServicesEndpointOptions, ServicesEndpointMiddleware, IServicesEndpointHandler,
                ServicesEndpointHandler, object?, IList<ServiceRegistration>>();

        RegisterJsonConverter(services);

        return services;
    }

    private static void RegisterJsonConverter(IServiceCollection services)
    {
        services.PostConfigure<ManagementOptions>(managementOptions =>
        {
            if (!managementOptions.SerializerOptions.Converters.OfType<ServiceRegistrationsJsonConverter>().Any())
            {
                managementOptions.SerializerOptions.Converters.Add(new ServiceRegistrationsJsonConverter());
            }
        });
    }
}
