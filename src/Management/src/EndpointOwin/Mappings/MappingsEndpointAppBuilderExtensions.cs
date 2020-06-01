// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        /// Add Route Mappings actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring refresh endpoint</param>
        /// <param name="apiExplorer">An <see cref="ApiExplorer"/> for iterating routes and their metadata</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Refresh Endpoint added</returns>
        public static IAppBuilder UseMappingActuator(this IAppBuilder builder, IConfiguration config, IApiExplorer apiExplorer, ILoggerFactory loggerFactory = null)
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

            IMappingsOptions options = new MappingsEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);

            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<IList<string>>>();
            return builder.Use<MappingsEndpointOwinMiddleware>(options, mgmtOptions, apiExplorer, loggerFactory?.CreateLogger<MappingsEndpointOwinMiddleware>());
        }
    }
}
