// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint;

internal sealed class ActuatorEndpointMapper
{
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptions;
    private readonly IEnumerable<IEndpointMiddleware> _middlewares;
    private readonly ILogger<ActuatorEndpointMapper> _logger;

    public ActuatorEndpointMapper(IOptionsMonitor<ManagementEndpointOptions> managementOptions, IEnumerable<IEndpointMiddleware> middlewares,
        ILogger<ActuatorEndpointMapper> logger)
    {
        _managementOptions = managementOptions;
        _middlewares = middlewares;
        _logger = logger;
    }

    public void Map(IEndpointRouteBuilder endpointRouteBuilder, ref ActuatorConventionBuilder conventionBuilder)
    {
        var collection = new HashSet<string>();

        conventionBuilder ??= new ActuatorConventionBuilder();
        // Map Default configured context
        IEnumerable<IEndpointMiddleware> middlewares = _middlewares.Where(m => m is not CloudFoundryEndpointMiddleware);
        MapEndpoints(endpointRouteBuilder, conventionBuilder, collection, _managementOptions.CurrentValue.Path, middlewares);

        // Map Cloudfoundry context
        if (Platform.IsCloudFoundry)
        {
            IEnumerable<IEndpointMiddleware> cfMiddlewares = _middlewares.Where(m => m is not ActuatorHypermediaEndpointMiddleware);
            MapEndpoints(endpointRouteBuilder, conventionBuilder, collection, ConfigureManagementEndpointOptions.DefaultCloudFoundryPath, cfMiddlewares);
        }
    }

    private void MapEndpoints(IEndpointRouteBuilder endpointRouteBuilder, ActuatorConventionBuilder conventionBuilder, HashSet<string> collection,
        string contextBasePath, IEnumerable<IEndpointMiddleware> middlewares)
    {
        foreach (IEndpointMiddleware middleware in middlewares)
        {
            Type middlewareType = middleware.GetType();
            RequestDelegate pipeline = endpointRouteBuilder.CreateApplicationBuilder().UseMiddleware(middlewareType).Build();
            HttpMiddlewareOptions endpointOptions = middleware.EndpointOptions;
            string contextPath = endpointOptions.GetPathMatchPattern(contextBasePath, _managementOptions.CurrentValue);

            if (collection.Add(contextPath))
            {
                IEndpointConventionBuilder builder = endpointRouteBuilder.MapMethods(contextPath, endpointOptions.AllowedVerbs, pipeline);
                conventionBuilder.Add(builder);
            }
            else
            {
                _logger.LogError("Skipping over duplicate path at {path}", contextPath);
            }
        }
    }
}
