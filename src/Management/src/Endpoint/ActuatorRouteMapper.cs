// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint;

internal sealed class ActuatorRouteMapper(
    IEnumerable<IEndpointMiddleware> middlewares, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : ActuatorMapper(middlewares, managementOptionsMonitor, loggerFactory)
{
    public void Map(IRouteBuilder routeBuilder)
    {
        ArgumentNullException.ThrowIfNull(routeBuilder);

        var routesMapped = new Dictionary<string, IEndpointMiddleware>(StringComparer.OrdinalIgnoreCase);

        foreach ((string routePattern, IEndpointMiddleware middleware) in GetEndpointsToMap())
        {
            if (!routesMapped.TryAdd(routePattern, middleware))
            {
                LogErrorForDuplicateRoute(routePattern, routesMapped[routePattern], middleware);
                continue;
            }

            RequestDelegate pipeline = CreatePipeline(routeBuilder, middleware);
            routeBuilder.MapRoute(routePattern, pipeline);
        }
    }

    private static RequestDelegate CreatePipeline(IRouteBuilder routeBuilder, IEndpointMiddleware middleware)
    {
        IApplicationBuilder applicationBuilder = routeBuilder.ApplicationBuilder.New();
        applicationBuilder.UseMiddleware(middleware.GetType());
        return applicationBuilder.Build();
    }
}
