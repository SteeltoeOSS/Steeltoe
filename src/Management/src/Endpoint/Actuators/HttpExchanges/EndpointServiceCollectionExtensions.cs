// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

public static class EndpointServiceCollectionExtensions
{
    /// <summary>
    /// Adds the HTTP exchanges actuator to the service container.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddHttpExchangesActuator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddCoreActuatorServicesAsSingleton<HttpExchangesEndpointOptions, ConfigureHttpExchangesEndpointOptions, HttpExchangesEndpointMiddleware,
                IHttpExchangesEndpointHandler, HttpExchangesEndpointHandler, object?, HttpExchangesResult>();

        services.AddDiagnosticsManager();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticObserver, HttpExchangesDiagnosticObserver>());

        services.TryAddSingleton<IHttpExchangesRepository>(serviceProvider =>
            serviceProvider.GetServices<IDiagnosticObserver>().OfType<HttpExchangesDiagnosticObserver>().Single());

        return services;
    }
}
