// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint.Hypermedia;
using System;
using System.Linq;

namespace Steeltoe.Management.EndpointOwin.Hypermedia
{
    public static class HypermediaActuatorEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Cloud Foundry actuator to OWIN Pipeline
        /// </summary>
        /// <param name="builder">Your OWIN <see cref="IAppBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for logging within the middleware</param>
        /// <returns>Your OWIN <see cref="IAppBuilder"/> with Cloud Foundry actuator attached</returns>
        public static IAppBuilder UseHypermediaActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IActuatorHypermediaOptions options;

            options = new HypermediaEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            var mgmt = mgmtOptions.OfType<ActuatorManagementOptions>().Single();
            mgmt.EndpointOptions.Add(options);

            var endpoint = new ActuatorEndpoint(options, mgmtOptions, loggerFactory?.CreateLogger<ActuatorEndpoint>());
            var logger = loggerFactory?.CreateLogger<ActuatorHypermediaEndpointOwinMiddleware>();
            return builder.Use<ActuatorHypermediaEndpointOwinMiddleware>(endpoint, mgmtOptions, logger);
        }
    }
}
