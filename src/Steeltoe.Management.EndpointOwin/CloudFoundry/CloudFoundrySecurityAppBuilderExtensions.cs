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
using Steeltoe.Management.Endpoint.CloudFoundry;

namespace Steeltoe.Management.EndpointOwin.CloudFoundry
{
    public static class CloudFoundrySecurityAppBuilderExtensions
    {
        /// <summary>
        /// Add Cloud Foundry security to actuator requests in the OWIN Pipeline
        /// </summary>
        /// <param name="builder">Your OWIN <see cref="IAppBuilder"/></param>
        /// <param name="config">Your application's <see cref="IConfiguration"/></param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for logging within the middleware</param>
        /// <returns>Your OWIN <see cref="IAppBuilder"/> with Cloud Foundry request security and CORS configured</returns>
        public static IAppBuilder UseCloudFoundrySecurityMiddleware(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new System.ArgumentNullException(nameof(config));
            }

            var logger = loggerFactory?.CreateLogger<CloudFoundrySecurityOwinMiddleware>();
            return builder.Use<CloudFoundrySecurityOwinMiddleware>(new CloudFoundryOptions(config), logger);
        }
    }
}
