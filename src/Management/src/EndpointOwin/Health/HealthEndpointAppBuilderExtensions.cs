// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Health
{
    public static class HealthEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add HealthCheck actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring health endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Health Endpoint added</returns>
        public static IAppBuilder UseHealthActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var mgmtOptions = ManagementOptions.Get(config);
            var options = new HealthEndpointOptions(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            return builder.UseHealthActuator(options, new DefaultHealthAggregator(), loggerFactory);
        }

        /// <summary>
        /// Add HealthCheck middleware to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="options"><see cref="IHealthOptions"/> for configuring Health endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Health Endpoint added</returns>
        public static IAppBuilder UseHealthActuator(this IAppBuilder builder, IHealthOptions options, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return builder.UseHealthActuator(options, new DefaultHealthAggregator(), loggerFactory);
        }

        /// <summary>
        /// Add HealthCheck middleware to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="options"><see cref="IHealthOptions"/> for configuring Health endpoint</param>
        /// <param name="aggregator"><see cref="IHealthAggregator"/> for determining how to report aggregate health of the application</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Health Endpoint added</returns>
        public static IAppBuilder UseHealthActuator(this IAppBuilder builder, IHealthOptions options, IHealthAggregator aggregator, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (aggregator == null)
            {
                throw new ArgumentNullException(nameof(aggregator));
            }

            return builder.UseHealthActuator(options, aggregator, GetDefaultHealthContributors(), loggerFactory);
        }

        /// <summary>
        /// Add HealthCheck middleware to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="options"><see cref="IHealthOptions"/> for configuring Health endpoint</param>
        /// <param name="aggregator"><see cref="IHealthAggregator"/> for determining how to report aggregate health of the application</param>
        /// <param name="contributors">A list of <see cref="IHealthContributor"/> to monitor for determining application health</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Health Endpoint added</returns>
        public static IAppBuilder UseHealthActuator(this IAppBuilder builder, IHealthOptions options, IHealthAggregator aggregator, IEnumerable<IHealthContributor> contributors, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (aggregator == null)
            {
                throw new ArgumentNullException(nameof(aggregator));
            }

            if (contributors == null)
            {
                throw new ArgumentNullException(nameof(contributors));
            }

            var mgmtOptions = ManagementOptions.Get();
            foreach (var mgmt in mgmtOptions)
            {
                if (!mgmt.EndpointOptions.Contains(options))
                {
                    mgmt.EndpointOptions.Add(options);
                }
            }

            var endpoint = new HealthEndpoint(options, aggregator, contributors, loggerFactory?.CreateLogger<HealthEndpoint>());
            var logger = loggerFactory?.CreateLogger<HealthEndpointOwinMiddleware>();
            return builder.Use<HealthEndpointOwinMiddleware>(endpoint, mgmtOptions, logger);
        }

        internal static List<IHealthContributor> GetDefaultHealthContributors()
        {
            return new List<IHealthContributor>
            {
                new DiskSpaceContributor()
            };
        }
    }
}