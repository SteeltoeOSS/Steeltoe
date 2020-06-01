// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Routing;
using System;

namespace Steeltoe.Management.Endpoint.Mappings
{
    public static class RouteBuilderExtensions
    {
        /// <summary>
        /// Add routes from RouteBuilder to mappings actuator
        /// </summary>
        /// <param name="builder">Your RouteBuilder builder</param>
        public static void AddRoutesToMappingsActuator(this IRouteBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var routeMappings = builder.ServiceProvider.GetService(typeof(IRouteMappings)) as IRouteMappings;
            foreach (var router in builder.Routes)
            {
                routeMappings.Routers.Add(router);
            }
        }
    }
}
