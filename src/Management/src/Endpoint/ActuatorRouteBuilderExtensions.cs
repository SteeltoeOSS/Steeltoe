// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint;

public static class ActuatorRouteBuilderExtensions
{
    /// <summary>
    /// Generic route builder extension for Actuators.
    /// </summary>
    /// <param name="endpoints">
    /// IEndpointRouteBuilder to Map route.
    /// </param>
    /// <param name="convention">
    /// A convention builder action that applies a convention to the whole collection.
    /// </param>
    /// <typeparam name="TEndpoint">
    /// Middleware for which the route is mapped.
    /// </typeparam>
    /// <exception cref="InvalidOperationException">
    /// When T is not found in service container.
    /// </exception>
    //public static void Map<TEndpoint>(this IEndpointRouteBuilder endpoints, Action<IEndpointConventionBuilder> convention = null)
    //    where TEndpoint : IEndpoint
    //{
    //    //MapActuatorEndpoint(endpoints, typeof(TEndpoint), convention);
    //}
    public static IEndpointConventionBuilder MapTheActuators(this IEndpointRouteBuilder endpoints, ActuatorConventionBuilder conventionBuilder = null)
    {
        var mapper = endpoints.ServiceProvider.GetService<ActuatorEndpointMapper>();
        mapper.Map(endpoints, conventionBuilder);
        return conventionBuilder;
      
    }

    //internal static void MapActuatorEndpoint(this IEndpointRouteBuilder endpoints, Type typeEndpoint, Action<IEndpointConventionBuilder> convention)
    //{
    //    ArgumentGuard.NotNull(endpoints);

    //    ConnectEndpointOptionsWithManagementOptions(endpoints);

    //    (Type middlewareType, Type optionsType) = LookupMiddleware(typeEndpoint);
    //    var options = endpoints.ServiceProvider.GetService(optionsType) as IEndpointOptions;
    //    IEnumerable<IManagementOptions> managementOptionsCollection = endpoints.ServiceProvider.GetServices<IManagementOptions>();

    //    foreach (IManagementOptions managementOptions in managementOptionsCollection)
    //    {
    //        if ((managementOptions is CloudFoundryManagementOptions && options is IActuatorHypermediaOptions) ||
    //            (managementOptions is ActuatorManagementOptions && options is ICloudFoundryOptions))
    //        {
    //            continue;
    //        }

    //        string fullPath = options.GetContextPath(managementOptions);

    //        RoutePattern pattern = RoutePatternFactory.Parse(fullPath);

    //        // only add middleware if the route hasn't already been mapped
    //        if (!endpoints.DataSources.Any(d => d.Endpoints.Any(ep => ep is RouteEndpoint endpoint && endpoint.RoutePattern.RawText == pattern.RawText)))
    //        {
    //            RequestDelegate pipeline = endpoints.CreateApplicationBuilder().UseMiddleware(middlewareType, managementOptions).Build();

    //            IEnumerable<string> allowedVerbs = options.AllowedVerbs ?? new List<string>
    //            {
    //                "Get"
    //            };

    //            IEndpointConventionBuilder conventionBuilder = endpoints.MapMethods(fullPath, allowedVerbs, pipeline);
    //            convention?.Invoke(conventionBuilder);
    //        }
    //    }
    //}

    //private static void ConnectEndpointOptionsWithManagementOptions(IEndpointRouteBuilder endpoints)
    //{
    //    IServiceProvider serviceProvider = endpoints.ServiceProvider;
    //    IEnumerable<IManagementOptions> managementOptions = serviceProvider.GetServices<IManagementOptions>();

    //    foreach (IEndpointOptions endpointOption in serviceProvider.GetServices<IEndpointOptions>())
    //    {
    //        foreach (IManagementOptions managementOption in managementOptions)
    //        {
    //            // hypermedia endpoint is not exposed when running cloudfoundry
    //            if (managementOption is CloudFoundryManagementOptions && endpointOption is HypermediaEndpointOptions)
    //            {
    //                continue;
    //            }

    //            if (!managementOption.EndpointOptions.Contains(endpointOption))
    //            {
    //                managementOption.EndpointOptions.Add(endpointOption);
    //            }
    //        }
    //    }
    //}
}

public class ActuatorConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders = new();
    public void Add(Action<EndpointBuilder> convention)
    {
        foreach(var builder in _builders)
        {
            builder.Add(convention);
        }
    }
    public void Add(IEndpointConventionBuilder builder)
    {
        _builders.Add(builder);
    }
}
