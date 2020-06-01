// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Steeltoe.Management.EndpointOwin.Trace
{
    public static class TraceEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Add Request Trace actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring thread dump endpoint</param>
        /// <param name="traceRepository">repository to put traces in</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Trace Endpoint added</returns>
        public static IAppBuilder UseTraceActuator(this IAppBuilder builder, IConfiguration config, ITraceRepository traceRepository = null, ILoggerFactory loggerFactory = null)
        {
           return builder.UseTraceActuator(config, traceRepository, loggerFactory, MediaTypeVersion.V1);
        }

        public static IAppBuilder UseTraceActuator(this IAppBuilder builder, IConfiguration config, ITraceRepository traceRepository, ILoggerFactory loggerFactory, MediaTypeVersion version)
        {
            switch (version)
            {
                case MediaTypeVersion.V1:
                    return builder.UseTraceActuatorComponents(config, traceRepository, loggerFactory);
                default:
                    return builder.UseHttpTraceActuator(config, null, loggerFactory);
            }
        }

        /// <summary>
        /// Add Http Request Trace actuator endpoint to OWIN Pipeline
        /// </summary>
        /// <param name="builder">OWIN <see cref="IAppBuilder" /></param>
        /// <param name="config"><see cref="IConfiguration"/> of application for configuring thread dump endpoint</param>
        /// <param name="traceRepository">repository to put traces in</param>
        /// <param name="loggerFactory">For logging within the middleware</param>
        /// <returns>OWIN <see cref="IAppBuilder" /> with Trace Endpoint added</returns>
        public static IAppBuilder UseHttpTraceActuator(this IAppBuilder builder, IConfiguration config, IHttpTraceRepository traceRepository, ILoggerFactory loggerFactory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            ITraceOptions options = new HttpTraceEndpointOptions(config);

            var mgmtOptions = ManagementOptions.Get(config);

            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            traceRepository = traceRepository ?? new HttpTraceDiagnosticObserver(options, loggerFactory?.CreateLogger<HttpTraceDiagnosticObserver>());
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)traceRepository);
            var endpoint = new HttpTraceEndpoint(options, traceRepository, loggerFactory?.CreateLogger<HttpTraceEndpoint>());
            var logger = loggerFactory?.CreateLogger<EndpointOwinMiddleware<HttpTraceEndpoint, HttpTraceResult>>();
            return builder.Use<EndpointOwinMiddleware<HttpTraceResult>>(endpoint, mgmtOptions, new List<HttpMethod> { HttpMethod.Get }, true, logger);
        }

        private static IAppBuilder UseTraceActuatorComponents(this IAppBuilder builder, IConfiguration config, ITraceRepository traceRepository, ILoggerFactory loggerFactory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            ITraceOptions options = new TraceEndpointOptions(config);

            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            traceRepository = traceRepository ?? new TraceDiagnosticObserver(options, loggerFactory?.CreateLogger<TraceDiagnosticObserver>());
            DiagnosticsManager.Instance.Observers.Add((IDiagnosticObserver)traceRepository);
            var endpoint = new TraceEndpoint(options, traceRepository, loggerFactory?.CreateLogger<TraceEndpoint>());
            var logger = loggerFactory?.CreateLogger<TraceEndpointOwinMiddleware>();
            return builder.Use<TraceEndpointOwinMiddleware>(endpoint, mgmtOptions, logger);
        }
    }
}
