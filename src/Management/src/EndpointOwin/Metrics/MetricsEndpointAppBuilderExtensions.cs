// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Metrics.Observer;
using System;

namespace Steeltoe.Management.EndpointOwin.Metrics
{
    public static class MetricsEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Metrics actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring metrics endpoint</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Metrics Endpoint added</returns>
        public static IAppBuilder UseMetricsActuator(this IAppBuilder builder, IConfiguration config, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return builder.UseMetricsActuator(config, OpenCensusStats.Instance, OpenCensusTags.Instance, loggerFactory);
        }

        /// <summary>
        /// Add Metrics actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring metrics endpoint</param>
        /// <param name="stats">Class for recording statistics - See also: <seealso cref="OpenCensusStats"/></param>
        /// <param name="tags">Class using for recording statistics</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Metrics Endpoint added</returns>
        public static IAppBuilder UseMetricsActuator(this IAppBuilder builder, IConfiguration config, IStats stats, ITags tags, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            IMetricsOptions options = new MetricsEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var hostObserver = new OwinHostingObserver(options, stats, tags, loggerFactory?.CreateLogger<OwinHostingObserver>());
            var clrObserver = new CLRRuntimeObserver(options, stats, tags, loggerFactory?.CreateLogger<CLRRuntimeObserver>());
            DiagnosticsManager.Instance.Observers.Add(hostObserver);
            DiagnosticsManager.Instance.Observers.Add(clrObserver);

            var clrSource = new CLRRuntimeSource();
            DiagnosticsManager.Instance.Sources.Add(clrSource);

            var endpoint = new MetricsEndpoint(options, stats, loggerFactory?.CreateLogger<MetricsEndpoint>());
            var logger = loggerFactory?.CreateLogger<MetricsEndpointOwinMiddleware>();
            return builder.Use<MetricsEndpointOwinMiddleware>(endpoint, mgmtOptions, logger);
        }
    }
}
