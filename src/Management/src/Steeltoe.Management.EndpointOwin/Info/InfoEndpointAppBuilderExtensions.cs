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
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Management.EndpointOwin.Info
{
    public static class InfoEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Info actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring info endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Info Endpoint added</returns>
        public static IAppBuilder UseInfoActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return builder.UseInfoActuator(config, GetDefaultInfoContributors(config, loggerFactory), loggerFactory);
        }

        /// <summary>
        /// Add Info actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring info endpoint</param>
        /// <param name="contributors">IInfo Contributors to collect into from</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Info Endpoint added</returns>
        public static IAppBuilder UseInfoActuator(this IAppBuilder builder, IConfiguration config, IList<IInfoContributor> contributors, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (contributors == null)
            {
                throw new ArgumentNullException(nameof(contributors));
            }

            var endpoint = new InfoEndpoint(new InfoOptions(config), contributors, loggerFactory?.CreateLogger<InfoEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<Dictionary<string, object>>>();
            return builder.Use<EndpointOwinMiddleware<Dictionary<string, object>>>(endpoint, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }

        private static IList<IInfoContributor> GetDefaultInfoContributors(IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            return new List<IInfoContributor>
                {
                    new GitInfoContributor(AppDomain.CurrentDomain.BaseDirectory + "\\git.properties", loggerFactory?.CreateLogger<GitInfoContributor>()),
                    new AppSettingsInfoContributor(config)
                };
        }
    }
}
