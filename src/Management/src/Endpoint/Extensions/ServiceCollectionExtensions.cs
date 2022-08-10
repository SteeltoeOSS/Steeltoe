// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Internal;

namespace Steeltoe.Management.Endpoint;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers mapping for an endpoint. This gets used when calling
    /// <see cref="ActuatorRouteBuilderExtensions.MapAllActuators(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, MediaTypeVersion)" />.
    /// </summary>
    /// <typeparam name="TEndpoint">
    /// The type of endpoint.
    /// </typeparam>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    public static IServiceCollection AddActuatorEndpointMapping<TEndpoint>(this IServiceCollection services)
        where TEndpoint : class, IEndpoint
    {
        services.AddSingleton(new EndpointMappingEntry
        {
            // new way compatible with .NET 6
            SetupConvention = (endpoints, conventionBuilder) => endpoints.Map<TEndpoint>(conventionBuilder)
        });

        return services;
    }
}
