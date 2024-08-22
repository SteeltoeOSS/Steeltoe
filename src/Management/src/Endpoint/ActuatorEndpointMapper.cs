// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint;

internal sealed class ActuatorEndpointMapper
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly ICollection<IEndpointMiddleware> _middlewares;
    private readonly ILogger<ActuatorEndpointMapper> _logger;

    public ActuatorEndpointMapper(IOptionsMonitor<ManagementOptions> managementOptionsMonitor, IEnumerable<IEndpointMiddleware> middlewares,
        ILogger<ActuatorEndpointMapper> logger)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(middlewares);
        ArgumentNullException.ThrowIfNull(logger);

        IEndpointMiddleware[] middlewareArray = middlewares.ToArray();
        ArgumentGuard.ElementsNotNull(middlewareArray);

        _managementOptionsMonitor = managementOptionsMonitor;
        _middlewares = middlewareArray;
        _logger = logger;
    }

    public void Map(IEndpointRouteBuilder endpointRouteBuilder, ActuatorConventionBuilder conventionBuilder)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);
        ArgumentNullException.ThrowIfNull(conventionBuilder);

        InnerMap(middleware => endpointRouteBuilder.CreateApplicationBuilder().UseMiddleware(middleware.GetType()).Build(),
            (middleware, requestPath, pipeline) =>
            {
                IEndpointConventionBuilder builder = endpointRouteBuilder.MapMethods(requestPath, middleware.EndpointOptions.AllowedVerbs, pipeline);
                conventionBuilder.Add(builder);
            });
    }

    public void Map(IRouteBuilder routeBuilder)
    {
        ArgumentNullException.ThrowIfNull(routeBuilder);

        InnerMap(middleware => routeBuilder.ApplicationBuilder.UseMiddleware(middleware.GetType()).Build(), (middleware, requestPath, pipeline) =>
        {
            foreach (string verb in middleware.EndpointOptions.AllowedVerbs)
            {
                routeBuilder.MapVerb(verb, requestPath, pipeline);
            }
        });
    }

    private void InnerMap(Func<IEndpointMiddleware, RequestDelegate> createPipeline, Action<IEndpointMiddleware, string, RequestDelegate> applyMapping)
    {
        var collection = new HashSet<string>();

        // Map Default configured context
        IEnumerable<IEndpointMiddleware> middlewares = _middlewares.Where(middleware => middleware is not CloudFoundryEndpointMiddleware);
        MapEndpoints(collection, _managementOptionsMonitor.CurrentValue.Path, middlewares, createPipeline, applyMapping);

        // Map Cloudfoundry context
        if (Platform.IsCloudFoundry)
        {
            IEnumerable<IEndpointMiddleware> cloudFoundryMiddlewares = _middlewares.Where(middleware => middleware is not ActuatorHypermediaEndpointMiddleware);
            MapEndpoints(collection, ConfigureManagementOptions.DefaultCloudFoundryPath, cloudFoundryMiddlewares, createPipeline, applyMapping);
        }
    }

    private void MapEndpoints(HashSet<string> collection, string? baseRequestPath, IEnumerable<IEndpointMiddleware> middlewares,
        Func<IEndpointMiddleware, RequestDelegate> createPipeline, Action<IEndpointMiddleware, string, RequestDelegate> applyMapping)
    {
        foreach (IEndpointMiddleware middleware in middlewares)
        {
            RequestDelegate pipeline = createPipeline(middleware);
            EndpointOptions endpointOptions = middleware.EndpointOptions;
            string requestPath = endpointOptions.GetPathMatchPattern(_managementOptionsMonitor.CurrentValue, baseRequestPath);

            if (collection.Add(requestPath))
            {
                applyMapping(middleware, requestPath, pipeline);
            }
            else
            {
                _logger.LogError("Skipping over duplicate path at {Path}", requestPath);
            }
        }
    }
}
