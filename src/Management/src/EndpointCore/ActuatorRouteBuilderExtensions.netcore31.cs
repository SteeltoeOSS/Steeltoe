// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint;

public static partial class ActuatorRouteBuilderExtensions
{
    /// <summary>
    /// Generic routebuilder extension for Actuators.
    /// </summary>
    /// <param name="endpoints">IEndpointRouteBuilder to Map route.</param>
    /// <param name="conventionBuilder">A convention builder that applies a convention to the whole collection. </param>
    /// <typeparam name="TEndpoint">Middleware for which the route is mapped.</typeparam>
    /// <exception cref="InvalidOperationException">When T is not found in service container.</exception>
    public static IEndpointConventionBuilder Map<TEndpoint>(this IEndpointRouteBuilder endpoints, EndpointCollectionConventionBuilder conventionBuilder)
        where TEndpoint : IEndpoint
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return MapActuatorEndpoint(endpoints, typeof(TEndpoint), conventionBuilder);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Maps all actuators that have been registered in <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="endpoints">The endpoint builder.</param>
    /// <returns>Endpoint convention builder.</returns>
    [Obsolete("Use MapAllActuators(this IEndpointRouteBuilder endpoints, Action<IEndpointConventionBuilder> convention) instead")]
    public static IEndpointConventionBuilder MapAllActuators(this IEndpointRouteBuilder endpoints)
    {
        var conventionBuilder = new EndpointCollectionConventionBuilder();

        foreach (var endpointEntry in endpoints.ServiceProvider.GetServices<EndpointMappingEntry>())
        {
            // Some actuators only work on some platforms. i.e. Windows and Linux
            // Some actuators have different implementation depending on the MediaTypeVersion
            // Previously those checks where performed here and when adding things to the IServiceCollection
            // Now all that logic is handled in the IServiceCollection setup; no need to keep code in two different places in sync

            // This function just takes what has been registered, and sets up the endpoints
            // This keeps this method flexible; new actuators that are added later should automatically become available
            endpointEntry.Setup(endpoints, conventionBuilder);
        }

        return conventionBuilder;
    }

    [Obsolete("MediaTypeVersion parameter is not used")]
    internal static IEndpointConventionBuilder MapActuatorEndpoint(this IEndpointRouteBuilder endpoints, Type typeEndpoint, EndpointCollectionConventionBuilder conventionBuilder = null)
    {
        if (endpoints == null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        ConnectEndpointOptionsWithManagementOptions(endpoints);

        var (middleware, optionsType) = LookupMiddleware(typeEndpoint);
        var options = endpoints.ServiceProvider.GetService(optionsType) as IEndpointOptions;
        var managementOptionsCollection = endpoints.ServiceProvider.GetServices<IManagementOptions>();
        var builder = conventionBuilder ?? new EndpointCollectionConventionBuilder();

        foreach (var managementOptions in managementOptionsCollection)
        {
            if ((managementOptions is CloudFoundryManagementOptions && options is IActuatorHypermediaOptions)
                || (managementOptions is ActuatorManagementOptions && options is ICloudFoundryOptions))
            {
                continue;
            }

            var fullPath = options.GetContextPath(managementOptions);

            var pattern = RoutePatternFactory.Parse(fullPath);

            // only add middleware if the route hasn't already been mapped
            if (!endpoints.DataSources.Any(d => d.Endpoints.Any(ep => ep is RouteEndpoint endpoint && endpoint.RoutePattern.RawText == pattern.RawText)))
            {
                var pipeline = endpoints.CreateApplicationBuilder()
                    .UseMiddleware(middleware, managementOptions)
                    .Build();
                var allowedVerbs = options.AllowedVerbs ?? new List<string> { "Get" };
                builder.AddConventionBuilder(endpoints.MapMethods(fullPath, allowedVerbs, pipeline));
            }
        }

        return builder;
    }
}
#endif
