using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Availability;
using Steeltoe.Management.Endpoint.Health;

namespace Steeltoe.Management.Grpc;
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services used by the Health actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddHealthGrpcActuatorServices(this IServiceCollection services)
    {
        services.AddHealthActuator(new HealthRegistrationsAggregator(), EndpointServiceCollectionExtensions.DefaultHealthContributors);
        services.TryAddScoped<IHealthEndpointHandler, HealthEndpointHandler>();
      //  services.TryAddScoped<IEndpointHandler<HealthEndpointRequest, HealthEndpointResponse>, HealthEndpointHandler>();
        services.TryAddSingleton<ApplicationAvailability>();
        return services;

    }
}
