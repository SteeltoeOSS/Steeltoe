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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint.Mappings;
using System;
using System.Collections.Generic;
using System.Web.Http.Description;

namespace Steeltoe.Management.EndpointOwin.Mappings
{
    public static class MappingsEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Route Mappings middleware to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring refresh endpoint</param>
        /// <param name="apiExplorer">An <see cref="ApiExplorer"/> for iterating routes and their metadata</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Refresh Endpoint added</returns>
        public static IAppBuilder UseMappingEndpointMiddleware(this IAppBuilder builder, IConfiguration config, IApiExplorer apiExplorer, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (apiExplorer == null)
            {
                throw new ArgumentNullException(nameof(apiExplorer));
            }

            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<IList<string>>>();
            return builder.Use<MappingsEndpointOwinMiddleware>(new MappingsOptions(config), apiExplorer, loggerFactory?.CreateLogger<MappingsEndpointOwinMiddleware>());
        }
    }
}
