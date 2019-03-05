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
