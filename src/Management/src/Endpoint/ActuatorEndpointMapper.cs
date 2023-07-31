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
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IList<IEndpointMiddleware> _middlewares;
    private readonly ILogger<ActuatorEndpointMapper> _logger;

    public ActuatorEndpointMapper(IOptionsMonitor<ManagementOptions> managementOptionsMonitor, IEnumerable<IEndpointMiddleware> middlewares,
        ILogger<ActuatorEndpointMapper> logger)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(middlewares);
        ArgumentGuard.NotNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _middlewares = middlewares.ToList();
        _logger = logger;
    }

    public void Map(IEndpointRouteBuilder endpointRouteBuilder, ActuatorConventionBuilder conventionBuilder)
    {
        ArgumentGuard.NotNull(endpointRouteBuilder);
        ArgumentGuard.NotNull(conventionBuilder);

        var collection = new HashSet<string>();

        // Map Default configured context
        IEnumerable<IEndpointMiddleware> middlewares = _middlewares.Where(middleware => middleware is not CloudFoundryEndpointMiddleware);
        MapEndpoints(endpointRouteBuilder, conventionBuilder, collection, _managementOptionsMonitor.CurrentValue.Path, middlewares);

        // Map Cloudfoundry context
        if (Platform.IsCloudFoundry)
        {
            IEnumerable<IEndpointMiddleware> cloudFoundryMiddlewares = _middlewares.Where(middleware => middleware is not ActuatorHypermediaEndpointMiddleware);
            MapEndpoints(endpointRouteBuilder, conventionBuilder, collection, ConfigureManagementOptions.DefaultCloudFoundryPath, cloudFoundryMiddlewares);
        }
    }

    private void MapEndpoints(IEndpointRouteBuilder endpointRouteBuilder, ActuatorConventionBuilder conventionBuilder, HashSet<string> collection,
        string baseRequestPath, IEnumerable<IEndpointMiddleware> middlewares)
    {
        foreach (IEndpointMiddleware middleware in middlewares)
        {
            Type middlewareType = middleware.GetType();
            RequestDelegate pipeline = endpointRouteBuilder.CreateApplicationBuilder().UseMiddleware(middlewareType).Build();
            EndpointOptions endpointOptions = middleware.EndpointOptions;
            string requestPath = endpointOptions.GetPathMatchPattern(_managementOptionsMonitor.CurrentValue, baseRequestPath);

            if (collection.Add(requestPath))
            {
                IEndpointConventionBuilder builder = endpointRouteBuilder.MapMethods(requestPath, endpointOptions.AllowedVerbs, pipeline);
                conventionBuilder.Add(builder);
            }
            else
            {
                _logger.LogError("Skipping over duplicate path at {path}", requestPath);
            }
        }
    }
}
