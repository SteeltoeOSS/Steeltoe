// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Management.Endpoint.Actuators.RouteMappings;

public static class RouteBuilderExtensions
{
    /// <summary>
    /// Adds routes from <see cref="IRouteBuilder" /> to the mappings actuator.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRouteBuilder" /> to add routes to.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="builder" /> so that additional calls can be chained.
    /// </returns>
    public static IRouteBuilder AddRoutesToMappingsActuator(this IRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var routerMappings = builder.ServiceProvider.GetRequiredService<RouterMappings>();

        foreach (IRouter router in builder.Routes)
        {
            routerMappings.Routers.Add(router);
        }

        return builder;
    }
}
