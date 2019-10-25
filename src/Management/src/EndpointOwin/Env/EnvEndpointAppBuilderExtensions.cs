// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint.Env;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Management.EndpointOwin.Env
{
    public static class EnvEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Environment actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring env endpoint and inclusion in response</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Env Endpoint added</returns>
        public static IAppBuilder UseEnvActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var hostingEnvironment = new GenericHostingEnvironment()
            {
                ApplicationName = config[HostDefaults.ApplicationKey] ?? AppDomain.CurrentDomain.FriendlyName,
                EnvironmentName = config[HostDefaults.EnvironmentKey] ?? EnvironmentName.Production,
                ContentRootPath = AppContext.BaseDirectory
            };
            hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);
            return builder.UseEnvActuator(config, hostingEnvironment, loggerFactory);
        }

        /// <summary>
        /// Add Environment middleware to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring env endpoint and inclusion in response</param>
        /// <param name="hostingEnvironment"><see cref="IHostingEnvironment"/> of the application</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Env Endpoint added</returns>
        public static IAppBuilder UseEnvActuator(this IAppBuilder builder, IConfiguration config, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (hostingEnvironment == null)
            {
                throw new ArgumentNullException(nameof(hostingEnvironment));
            }

            IEnvOptions options = new EnvEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                if (!mgmt.EndpointOptions.Contains(options))
                {
                    mgmt.EndpointOptions.Add(options);
                }
            }

            var endpoint = new EnvEndpoint(options, config, hostingEnvironment, loggerFactory?.CreateLogger<EnvEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<EnvironmentDescriptor>>();
            return builder.Use<EndpointOwinMiddleware<EnvironmentDescriptor>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }
    }
}
