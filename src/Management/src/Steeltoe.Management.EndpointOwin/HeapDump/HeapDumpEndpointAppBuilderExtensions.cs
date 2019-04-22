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

            var options = new HeapDumpOptions(config);
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
            return builder.Use<HeapDumpEndpointOwinMiddleware>(endpoint, logger);
        }
    }
}
