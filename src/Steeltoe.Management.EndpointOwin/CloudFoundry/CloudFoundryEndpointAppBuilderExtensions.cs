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
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry
{
    public static class CloudFoundryEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Cloud Foundry actuator to OWIN Pipeline
        /// </summary>
        /// <param name="builder">Your OWIN <see cref="IAppBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="mgmtOptions">Shared management options</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for logging within the middleware</param>
        /// <returns>Your OWIN <see cref="IAppBuilder"/> with Cloud Foundry actuator attached</returns>
        public static IAppBuilder UseCloudFoundryActuator(this IAppBuilder builder, IConfiguration config, IEnumerable<IManagementOptions> mgmtOptions, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (mgmtOptions == null)
            {
                mgmtOptions = ManagementOptions.Get(config);
            }

            var cloudFoundryOptions = new CloudFoundryEndpointOptions(config);
            var mgmt = mgmtOptions.OfType<CloudFoundryManagementOptions>().Single();
            mgmt.EndpointOptions.Add(cloudFoundryOptions);

            var endpoint = new CloudFoundryEndpoint(cloudFoundryOptions, mgmtOptions, loggerFactory?.CreateLogger<CloudFoundryEndpoint>());
            var logger = loggerFactory?.CreateLogger<CloudFoundryEndpointOwinMiddleware>();
            return builder.Use<CloudFoundryEndpointOwinMiddleware>(endpoint, mgmtOptions, logger);
        }


        public static IAppBuilder UseCloudFoundryActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            return builder.UseCloudFoundryActuator(config, mgmtOptions: null, loggerFactory: loggerFactory);
        }
    }
}
