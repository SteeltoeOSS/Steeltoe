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
using Steeltoe.Management.Endpoint.Discovery;
using Steeltoe.Management.Endpoint.Refresh;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Management.EndpointOwin.Refresh
{
    public static class RefreshEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add (Config) Refresh actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring refresh endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Refresh Endpoint added</returns>
        public static IAppBuilder UseRefreshActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IRefreshOptions options = new RefreshEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var endpoint = new RefreshEndpoint(options, config, loggerFactory?.CreateLogger<RefreshEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<IList<string>>>();
            return builder.Use<EndpointOwinMiddleware<IList<string>>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }
    }
}
