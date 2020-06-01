// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Management.Endpoint.HeapDump;
using System;

namespace Steeltoe.Management.EndpointOwin.HeapDump
{
    public static class HeapDumpEndpointAppBuilderExtensions
    {
        /// <summary>
        /// Adds actuator endpoint providing Heap Dumps to OWIN pipeline
        /// </summary>
        /// <param name="builder">Your <see cref="IAppBuilder"/></param>
        /// <param name="config"><see cref="IConfiguration"/> for configuring the endpoint</param>
        /// <param name="applicationPathOnDisk">Provide the path to the app directory if heap dumps are failing due to access restrictions</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for logging inside the middleware and its components</param>
        /// <returns>Your <see cref="IAppBuilder"/> with Heap Dump middleware attached</returns>
        public static IAppBuilder UseHeapDumpActuator(this IAppBuilder builder, IConfiguration config, string applicationPathOnDisk = null, ILoggerFactory loggerFactory = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IHeapDumpOptions options = new HeapDumpEndpointOptions(config);
            var mgmtOptions = ManagementOptions.Get(config);
            foreach (var mgmt in mgmtOptions)
            {
                mgmt.EndpointOptions.Add(options);
            }

            var heapDumper = new HeapDumper(options, applicationPathOnDisk, loggerFactory?.CreateLogger<HeapDumper>());
            return builder.UseHeapDumpActuator(options, heapDumper, loggerFactory);
        }

        /// <summary>
        /// Adds OWIN Middleware for providing Heap Dumps to OWIN pipeline
        /// </summary>
        /// <param name="builder">Your <see cref="IAppBuilder"/></param>
        /// <param name="options"><see cref="IHeapDumpOptions"/> to configure the endpoint</param>
        /// <param name="heapDumper"><see cref="HeapDumper"/> or other implementer of <see cref="IHeapDumper"/> for retrieving a heap dump</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for logging inside the middleware and its components</param>
        /// <returns>Your <see cref="IAppBuilder"/> with Heap Dump middleware attached</returns>
        public static IAppBuilder UseHeapDumpActuator(this IAppBuilder builder, IHeapDumpOptions options, IHeapDumper heapDumper, ILoggerFactory loggerFactory = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (heapDumper == null)
            {
                throw new ArgumentNullException(nameof(heapDumper));
            }

            var endpoint = new HeapDumpEndpoint(options, heapDumper, loggerFactory?.CreateLogger<HeapDumpEndpoint>());
            var logger = loggerFactory?.CreateLogger<HeapDumpEndpointOwinMiddleware>();
            return builder.Use<HeapDumpEndpointOwinMiddleware>(endpoint, ManagementOptions.Get(), logger);
        }
    }
}
