// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Info.Contributor;
using System;
using System.Collections.Generic;
using System.IO;
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

            var mgmtOptions = ManagementOptions.Get(config);
            var options = new InfoEndpointOptions(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var endpoint = new InfoEndpoint(options, contributors, loggerFactory?.CreateLogger<InfoEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<Dictionary<string, object>>>();
            return builder.Use<EndpointOwinMiddleware<Dictionary<string, object>>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }

        private static IList<IInfoContributor> GetDefaultInfoContributors(IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            var gitInfoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "git.properties");
            return new List<IInfoContributor>
                {
                    new GitInfoContributor(gitInfoPath, loggerFactory?.CreateLogger<GitInfoContributor>()),
                    new AppSettingsInfoContributor(config)
                };
        }
    }
}
