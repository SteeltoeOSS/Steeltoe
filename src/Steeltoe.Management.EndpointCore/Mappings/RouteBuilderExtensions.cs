// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
