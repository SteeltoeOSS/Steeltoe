// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            var mgmtOptions = ManagementOptions.Get(config);
            var cloudFoundryOptions = new CloudFoundryEndpointOptions(config);
            var logger = loggerFactory?.CreateLogger<CloudFoundrySecurityOwinMiddleware>();
            return builder.Use<CloudFoundrySecurityOwinMiddleware>(cloudFoundryOptions, mgmtOptions, logger);
        }
    }
}
